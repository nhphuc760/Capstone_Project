using System;
using System.Collections.Generic;
using Game.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hotbar 5 slot (phím 1 → 5).
/// Dữ liệu lấy trực tiếp từ NetworkInventory.Hotbar — luôn đồng bộ với Player.
/// </summary>
public class ToolKit : MonoBehaviour
{
    public const int SLOT_COUNT = NetworkInventory.HotbarCapacity;

    [Header("References")]
    [SerializeField] private NetworkInventory inventory;
    [SerializeField] ItemToolKitUse useItemHandler;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private ItemTooltip tooltip;



    [Header("Drag Visual (optional)")]
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private GameObject dragIconPrefab;
    //[SerializeField] PresentItem .

    private readonly List<InventorySlotUI> slots = new();
    private bool isInitialized;
    private int selectedHotbarIndex = -1;
    public event Action<int> OnLocalSelectedChanged;

    // Drag state
    private bool isDragging;
    private int dragSourceIndex = -1;          // index trong hotbar
    private bool dragFromInventory;
    private int dragInventoryIndex = -1;       // nếu kéo từ inventory
    private GameObject dragIconInstance;

    public NetworkInventory Inventory => inventory;
    public ItemTooltip Tooltip => tooltip;



    private void Start()
    {
        InitializeSlots();
        if (tooltip != null) tooltip.Hide();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
        EndDrag();
    }

    private void Update()
    {
        HandleHotkeyInput();
        UpdateDragVisual();
        HandleUseItem();
    }

    #region Init & Refresh

    private void InitializeSlots()
    {
        if (isInitialized) return;
        slots.Clear();

        if (slotsParent != null && slotsParent.childCount > 0)
        {
            for (int i = 0; i < Mathf.Min(SLOT_COUNT, slotsParent.childCount); i++)
            {
                var slot = slotsParent.GetChild(i).GetComponent<InventorySlotUI>();
                if (slot != null)
                {
                    slot.Initialize(i, null);
                    slots.Add(slot);
                }
            }
        }
        else if (slotPrefab != null)
        {
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.Initialize(i, null);
                slots.Add(slot);
            }
        }

        foreach (var slot in slots)
        {
            var handler = slot.GetComponent<ToolKitSlotDragHandler>();
            if (handler == null)
                handler = slot.gameObject.AddComponent<ToolKitSlotDragHandler>();
            handler.Setup(this, slot);
        }

        isInitialized = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventory == null || !isInitialized) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var item = inventory.GetHotbarSlot(i);
            var data = inventory.GetHotbarItemData(i);
            slots[i].SetItem(item, data);
        }
    }

    public void SetInventory(NetworkInventory inv)
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        inventory = inv;

        if (inventory != null && isActiveAndEnabled)
            inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    #endregion

    #region Hotkey 1-5

    void HandleUseItem()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RequestUseSlot(selectedHotbarIndex);
        }
    }

    private void HandleHotkeyInput()
    {
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                SelectHotbarSlot(i);
            }
        }
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        if (selectedHotbarIndex >= 0 && selectedHotbarIndex < slots.Count)
            slots[selectedHotbarIndex].SetSelected(false);

        selectedHotbarIndex = index;
        slots[index].SetSelected(true);
        OnLocalSelectedChanged?.Invoke(selectedHotbarIndex);
    }

    #endregion

    #region Drag & Drop

    public void BeginDragFromToolKit(InventorySlotUI source)
    {
        if (source == null || source.IsEmpty || inventory == null) return;

        isDragging = true;
        dragFromInventory = false;
        dragSourceIndex = source.SlotIndex;
        dragInventoryIndex = -1;

        CreateDragIcon(source.CurrentData);
    }

    /// <summary>
    /// Gọi từ InventoryUI khi bắt đầu kéo 1 slot inventory.
    /// Chỉ set state logic, KHÔNG tạo icon (InventoryUI đã tạo icon rồi).
    /// </summary>
    public void BeginDragFromInventory(int invSlotIndex, ItemSO data)
    {
        if (inventory == null || data == null) return;

        isDragging = true;
        dragFromInventory = true;
        dragSourceIndex = -1;
        dragInventoryIndex = invSlotIndex;

        // Không gọi CreateDragIcon — tránh tạo icon thứ 2 bị kẹt trên chuột.
        // Icon visual do InventoryUI quản lý.
    }

    public void EndDragOnToolKitSlot(InventorySlotUI target)
    {
        if (!isDragging || target == null || inventory == null)
        {
            EndDrag();
            return;
        }

        int targetIndex = target.SlotIndex;

        if (dragFromInventory)
        {
            // Inventory → Hotbar
            RequestMoveInventoryToHotbar(dragInventoryIndex, targetIndex);
        }
        else
        {
            // Hotbar → Hotbar (swap)
            if (dragSourceIndex != targetIndex)
                RequestSwapHotbar(dragSourceIndex, targetIndex);
        }

        EndDrag();
    }

    public void EndDragOnInventorySlot(int invTargetIndex)
    {
        if (!isDragging || inventory == null)
        {
            EndDrag();
            return;
        }

        if (!dragFromInventory && dragSourceIndex >= 0)
        {
            // Hotbar → Inventory
            RequestMoveHotbarToInventory(dragSourceIndex, invTargetIndex);
        }

        EndDrag();
    }

    public void CancelDrag() => EndDrag();

    private void EndDrag()
    {
        isDragging = false;
        dragSourceIndex = -1;
        dragInventoryIndex = -1;
        dragFromInventory = false;

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
            var img = dragIconInstance.GetComponent<UnityEngine.UI.Image>();
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
            var img = dragIconInstance.AddComponent<UnityEngine.UI.Image>();
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

    #region Gửi lệnh lên NetworkInventory

    private void RequestUseSlot(int selectedIndex)
    {
        if (inventory.Object.HasStateAuthority)
        {
            useItemHandler.ServerUse(selectedIndex);
        }
        else
        {
            useItemHandler.RPC_RequestUse(selectedIndex);
        }
    }

    private void RequestMoveInventoryToHotbar(int invIndex, int hotbarIndex)
    {
        if (inventory.Object.HasStateAuthority)
            inventory.MoveInventoryToHotbar(invIndex, hotbarIndex);
        else
            inventory.RPC_MoveInventoryToHotbar(invIndex, hotbarIndex);
    }

    private void RequestMoveHotbarToInventory(int hotbarIndex, int invIndex)
    {
        if (inventory.Object.HasStateAuthority)
            inventory.MoveHotbarToInventory(hotbarIndex, invIndex);
        else
            inventory.RPC_MoveHotbarToInventory(hotbarIndex, invIndex);
    }

    private void RequestSwapHotbar(int a, int b)
    {
        if (inventory.Object.HasStateAuthority)
            inventory.SwapHotbarSlots(a, b);
        else
            inventory.RPC_SwapHotbarSlots(a, b);
    }

    #endregion
}

/// <summary>
/// Drag + pointer handler cho từng slot của ToolKit.
/// </summary>
public class ToolKitSlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private ToolKit toolkit;
    private InventorySlotUI slot;

    public void Setup(ToolKit toolkit, InventorySlotUI slot)
    {
        this.toolkit = toolkit;
        this.slot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (toolkit == null || slot == null || slot.IsEmpty) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        toolkit.BeginDragFromToolKit(slot);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (toolkit == null) return;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            var s = r.gameObject.GetComponent<InventorySlotUI>()
                    ?? r.gameObject.GetComponentInParent<InventorySlotUI>();
            if (s == null) continue;

            // Slot thuộc ToolKit?
            if (s.GetComponent<ToolKitSlotDragHandler>() != null)
            {
                toolkit.EndDragOnToolKitSlot(s);
                return;
            }

            // Slot thuộc Inventory?
            if (s.GetComponent<InventorySlotDragHandler>() != null)
            {
                toolkit.EndDragOnInventorySlot(s.SlotIndex);
                return;
            }
        }

        toolkit.CancelDrag();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toolkit?.Tooltip != null && slot != null)
            toolkit.Tooltip.Show(slot.CurrentItem, slot.CurrentData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        toolkit?.Tooltip?.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (toolkit == null || slot == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            toolkit.SelectHotbarSlot(slot.SlotIndex);
    }
}
