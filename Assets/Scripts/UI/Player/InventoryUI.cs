using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour, IPlayerUI
{
    [Header("References")]
    [SerializeField] private NetworkInventory inventory;
    [SerializeField] private Transform slotsParent;          // Parent chứa các InventorySlotUI
    [SerializeField] private InventorySlotUI slotPrefab;    // Prefab của 1 slot (nếu muốn generate runtime)

    [Header("Selected")]
    InventorySlotUI selectedSlot;
    InventoryItem selectedItem;

    [Header("ToolTip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private readonly List<InventorySlotUI> slots = new();
    private bool isInitialized;

    private void Awake()
    {
        // Đăng ký với hệ thống UI của Player
        var playerUI = GetComponentInParent<PlayerUIComponent>();
        if (playerUI != null)
            playerUI.RegisterPlayerUI(PlayerUIComponent.OpenUI.Inventory, this);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Khi mở inventory thì refresh ngay
        if (inventory != null)
            RefreshUI();
        


    }


    public void OnSlotClicked(InventorySlotUI slot)
    {
        // Bỏ chọn slot cũ
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Chọn slot mới
        selectedSlot = slot;
        selectedItem = slot.CurrentItem;

        selectedSlot.SetSelected(true);       

        Debug.Log($"Đã chọn slot {slot.SlotIndex} | Item: {selectedItem.itemID}");
    }

    public void DeselectSlot()
    {
        if(selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            selectedItem = default;

            Debug.Log("Hủy chọn");
        }
    }

    public void ShowTooltip(InventorySlotUI slot)
    {
        if (tooltipPanel == null) return;

        if (slot.CurrentItem.IsEmpty)
        {
            HideTooltip();
            return;
        }

        tooltipPanel.SetActive(true);

        var itemSO = slot.CurrentData;

        string name = itemSO.ItemName;
        string desc = itemSO.Description;

        if (string.IsNullOrEmpty(desc))
        {
            tooltipText.text = $"<b><size=110%>{name}</size></b>";
        }
        else
        {
            tooltipText.text = $"<b><size=110%>{name}</size></b>\n{desc}";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());

        Vector3 offset = new Vector3(130f, -50f, 0f);
        tooltipPanel.transform.position = Input.mousePosition + offset;

    }
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void UseSelectedItem()
    {
        if (selectedItem.IsEmpty)
        {
            Debug.Log("Chưa chọn item nào!");
            return;
        }

        Debug.Log($"Sử dụng: {selectedItem.itemID}");
        // Viết logic sử dụng của bạn ở đây
    }
    private void Start()
    {
        InitializeSlots();
        HideTooltip();
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            
            //remove 1
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Q))
        {
            //remove stack
        }
    }

    private void InitializeSlots()
    {
        if (isInitialized) return;

        // Cách 1: Lấy các slot đã có sẵn trong hierarchy
        if (slotsParent != null && slotsParent.childCount != 0)
        {
            slots.Clear();
            for (int i = 0; i < slotsParent.childCount; i++)
            {
                var slot = slotsParent.GetChild(i).GetComponent<InventorySlotUI>();
                if (slot != null)
                {
                    slot.Initialize(i, this);
                    slots.Add(slot);
                }
            }
        }
        // Cách 2: Generate runtime từ prefab (nếu bạn muốn)
        else if (slotPrefab != null && inventory != null)
        {
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.Initialize(i, this);
                slots.Add(slot);
            }
        }

        isInitialized = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventory == null) return;

        // Đảm bảo slots đã được init
        if (!isInitialized)
            InitializeSlots();

        int count = Mathf.Min(slots.Count, inventory.Capacity);

        for (int i = 0; i < count; i++)
        {
            InventoryItem item = inventory.GetSlot(i);
            ItemSO data = inventory.GetItemData(i);

            slots[i].SetItem(item, data);
        }

        // Clear các slot thừa (nếu có)
        for (int i = count; i < slots.Count; i++)
        {
            slots[i].Clear();
        }
    }

    // Gọi từ bên ngoài khi inventory thay đổi (ví dụ sau khi Add/Remove)
    public void OnInventoryChanged()
    {
        Debug.Log("OnInventoryChanged called in InventoryUI");
        if (gameObject.activeInHierarchy)
            RefreshUI();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Optional: dùng nếu bạn muốn gán inventory runtime
    public void SetInventory(NetworkInventory inv)
    {
        inventory = inv;
        RefreshUI();
    }
}