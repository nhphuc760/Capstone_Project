using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour, IPlayerUI
{
    [Header("References")]
    [SerializeField] private NetworkInventory inventory;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private ToolKit toolKit;

    [Header("Selected")]
    private InventorySlotUI selectedSlot;
    private InventoryItem selectedItem;

    [Header("Tooltip")]
    [SerializeField] private ItemTooltip tooltip;

    [Header("Drag Visual (optional)")]
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private GameObject dragIconPrefab;

    private readonly List<InventorySlotUI> slots = new();
    private bool isInitialized;

    // Drag state
    private bool isDragging;
    private int dragSourceIndex = -1;
    private GameObject dragIconInstance;

    public ItemTooltip Tooltip => tooltip;
    public ToolKit ToolKit => toolKit;
    public NetworkInventory Inventory => inventory;

    private void Awake()
    {
        var playerUI = GetComponentInParent<PlayerUIComponent>();
        if (playerUI != null)
            playerUI.RegisterPlayerUI(PlayerUIComponent.OpenUI.Inventory, this);

        gameObject.SetActive(false);
    }

    private void Start()
    {
        InitializeSlots();
        if (tooltip != null) tooltip.Hide();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += OnInventoryChanged;
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;

        CleanupDrag();
        tooltip?.Hide();
    }

    private void Update()
    {
        UpdateDragVisual();
    }

    #region Slot Init & Refresh

    private void InitializeSlots()
    {
        if (isInitialized) return;

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

                    var handler = slot.GetComponent<InventorySlotDragHandler>();
                    if (handler == null)
                        handler = slot.gameObject.AddComponent<InventorySlotDragHandler>();
                    handler.Setup(this, slot);
                }
            }
        }
        else if (slotPrefab != null && inventory != null)
        {
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.Initialize(i, this);
                slots.Add(slot);

                var handler = slot.GetComponent<InventorySlotDragHandler>();
                if (handler == null)
                    handler = slot.gameObject.AddComponent<InventorySlotDragHandler>();
                handler.Setup(this, slot);
            }
        }

        isInitialized = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventory == null) return;
        if (!isInitialized) InitializeSlots();

        int count = Mathf.Min(slots.Count, inventory.Capacity);
        for (int i = 0; i < count; i++)
        {
            slots[i].SetItem(inventory.GetSlot(i), inventory.GetItemData(i));
        }
        for (int i = count; i < slots.Count; i++)
            slots[i].Clear();
    }

    public void OnInventoryChanged()
    {
        if (gameObject.activeInHierarchy)
            RefreshUI();
    }

    public void SetInventory(NetworkInventory inv)
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;

        inventory = inv;

        if (inventory != null && isActiveAndEnabled)
            inventory.OnInventoryChanged += OnInventoryChanged;

        RefreshUI();
    }

    #endregion

    #region Click / Select / Tooltip

    public void OnSlotClicked(InventorySlotUI slot)
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedItem = slot.CurrentItem;
        selectedSlot.SetSelected(true);

        Debug.Log($"Đã chọn slot {slot.SlotIndex} | Item: {selectedItem.itemID}");
    }

    public void DeselectSlot()
    {
        if (selectedSlot == null) return;
        selectedSlot.SetSelected(false);
        selectedSlot = null;
        selectedItem = default;
    }

    public void ShowTooltip(InventorySlotUI slot)
    {
        if (tooltip == null) return;
        if (slot == null || slot.CurrentItem.IsEmpty)
        {
            tooltip.Hide();
            return;
        }
        tooltip.Show(slot.CurrentItem, slot.CurrentData);
    }

    public void HideTooltip() => tooltip?.Hide();

    public void UseSelectedItem()
    {
        if (selectedItem.IsEmpty)
        {
            Debug.Log("Chưa chọn item nào!");
            return;
        }
        Debug.Log($"Sử dụng: {selectedItem.itemID}");
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        DeselectSlot();
        CleanupDrag();
        tooltip?.Hide();
    }

    #endregion

    #region Drag & Drop

    public void BeginDrag(InventorySlotUI source)
    {
        if (source == null || source.IsEmpty || inventory == null) return;

        isDragging = true;
        dragSourceIndex = source.SlotIndex;

        CreateDragIcon(source.CurrentData);

        // Báo cho ToolKit biết đang kéo từ Inventory
        toolKit?.BeginDragFromInventory(source.SlotIndex, source.CurrentData);
    }

    public void EndDragOnSlot(InventorySlotUI target)
    {
        if (!isDragging || inventory == null)
        {
            CleanupDrag();
            return;
        }

        if (target == null)
        {
            CleanupDrag();
            return;
        }

        // Thả lên slot của ToolKit?
        if (target.GetComponent<ToolKitSlotDragHandler>() != null)
        {
            toolKit?.EndDragOnToolKitSlot(target);
            CleanupDrag();
            return;
        }

        // Thả lên slot Inventory khác → swap trong inventory
        if (target.GetComponent<InventorySlotDragHandler>() != null)
        {
            int targetIndex = target.SlotIndex;
            if (dragSourceIndex != targetIndex)
            {
                if (inventory.Object.HasStateAuthority)
                    inventory.SwapInventorySlots(dragSourceIndex, targetIndex);
                else
                    inventory.RPC_SwapInventorySlots(dragSourceIndex, targetIndex);
            }
        }

        CleanupDrag();
    }

    public void CancelDrag()
    {
        CleanupDrag();
    }

    /// <summary>
    /// Luôn dọn cả InventoryUI và ToolKit để icon kéo không bị kẹt trên chuột.
    /// </summary>
    private void CleanupDrag()
    {
        EndDrag();
        toolKit?.CancelDrag();
    }

    private void EndDrag()
    {
        isDragging = false;
        dragSourceIndex = -1;

        if (dragIconInstance != null)
        {
            Destroy(dragIconInstance);
            dragIconInstance = null;
        }
    }

    private void CreateDragIcon(ItemSO data)
    {
        if (dragCanvas == null || data == null) return;

        if (dragIconPrefab != null)
        {
            dragIconInstance = Instantiate(dragIconPrefab, dragCanvas.transform);
            var img = dragIconInstance.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = data.Icon;
                img.raycastTarget = false;
            }
        }
        else
        {
            dragIconInstance = new GameObject("DragIcon");
            dragIconInstance.transform.SetParent(dragCanvas.transform, false);
            var img = dragIconInstance.AddComponent<Image>();
            img.sprite = data.Icon;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(64, 64);
        }
    }

    private void UpdateDragVisual()
    {
        if (!isDragging || dragIconInstance == null) return;
        dragIconInstance.transform.position = Input.mousePosition;
    }

    #endregion
}

/// <summary>
/// Drag handler gắn trên slot của Inventory.
/// </summary>
public class InventorySlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryUI inventoryUI;
    private InventorySlotUI slot;

    public void Setup(InventoryUI ui, InventorySlotUI slot)
    {
        this.inventoryUI = ui;
        this.slot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || slot == null || slot.IsEmpty) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        inventoryUI.BeginDrag(slot);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryUI == null) return;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        InventorySlotUI target = null;
        foreach (var r in results)
        {
            var s = r.gameObject.GetComponent<InventorySlotUI>()
                    ?? r.gameObject.GetComponentInParent<InventorySlotUI>();
            if (s != null)
            {
                target = s;
                break;
            }
        }

        if (target != null)
            inventoryUI.EndDragOnSlot(target);
        else
            inventoryUI.CancelDrag();
    }
}
