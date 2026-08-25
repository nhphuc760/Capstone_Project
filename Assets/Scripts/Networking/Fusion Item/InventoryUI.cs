using System.Windows.Forms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public enum InventoryFilter
{
    All,
    Weapon,
    Consumable
}

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Slots")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Settings")]
    [SerializeField] private int maxSlots = 30;
    [SerializeField] private Button allItem;
    [SerializeField] private Button weaponItem;
    [SerializeField] private Button consumableItem;

    private InventorySlotUI[] slots;

    private NetworkInventory inventory;

    private InventoryFilter currentFilter =
        InventoryFilter.All;

    private bool isOpen;

    private void Awake()
    {
        CreateSlots();

        inventoryPanel.SetActive(false);

        isOpen = false;

        allItem.onClick.AddListener(() => SetFilter(InventoryFilter.All));
        weaponItem.onClick.AddListener(() => SetFilter(InventoryFilter.Weapon));
        consumableItem.onClick.AddListener(() => SetFilter(InventoryFilter.Consumable));
    }


    private void Update()
    {
        // OPEN / CLOSE INVENTORY
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }


        // FIND INVENTORY
        if (inventory == null)
        {
            FindInventory();
        }


        // TEMPORARY REFRESH
        // Sau này nên đổi sang event/change detection
        if (isOpen && inventory != null)
        {
            Refresh();
        }
    }


    // =========================================================
    // FIND LOCAL INVENTORY
    // =========================================================

    private void FindInventory()
    {
        NetworkInventory[] inventories =
            FindObjectsByType<NetworkInventory>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);


        foreach (NetworkInventory inv in inventories)
        {
            if (inv.Object == null)
                continue;


            if (!inv.Object.HasInputAuthority)
                continue;


            inventory = inv;


            Debug.Log(
                $"InventoryUI connected to {inventory.name}"
            );


            Refresh();

            return;
        }
    }


    // =========================================================
    // CREATE SLOTS
    // =========================================================

    private void CreateSlots()
    {
        slots =
            new InventorySlotUI[maxSlots];


        for (int i = 0; i < maxSlots; i++)
        {
            InventorySlotUI slot =
                Instantiate(
                    slotPrefab,
                    slotContainer
                );


            slot.name =
                $"Slot_{i}";


            slot.Clear();


            slots[i] = slot;
        }
    }


    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        inventoryPanel.SetActive(isOpen);


        if (isOpen)
        {
            if (inventory == null)
                FindInventory();


            Refresh();
        }
    }


    public void OpenInventory()
    {
        isOpen = true;

        inventoryPanel.SetActive(true);


        if (inventory == null)
            FindInventory();


        Refresh();
    }


    public void CloseInventory()
    {
        isOpen = false;

        inventoryPanel.SetActive(false);
    }


    // =========================================================
    // FILTER
    // =========================================================

    public void SetFilter(InventoryFilter filter)
    {
        currentFilter = filter;

        Refresh();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (inventory == null)
            return;


        // Clear UI
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Clear();
        }


        int uiIndex = 0;


        // Read NetworkInventory
        for (int i = 0;
             i < inventory.Capacity;
             i++)
        {
            InventoryItem slotData =
                inventory.GetSlot(i);


            if (slotData.IsEmpty)
                continue;


            ItemSO item =
                inventory.GetItemData(i);


            if (item == null)
                continue;


            // Check filter
            if (!MatchesFilter(item))
                continue;


            // Prevent UI overflow
            if (uiIndex >= slots.Length)
                break;


            slots[uiIndex].SetItem(
                item,
                slotData.amount
            );


            uiIndex++;
        }
    }


    // =========================================================
    // CHECK FILTER
    // =========================================================

    private bool MatchesFilter(ItemSO item)
    {
        if (item == null)
            return false;


        switch (currentFilter)
        {
            case InventoryFilter.All:

                return true;


            case InventoryFilter.Weapon:

                return item.ItemType ==
                       ItemType.Weapon;


            case InventoryFilter.Consumable:

                return item.ItemType ==
                       ItemType.Consumable;


            default:

                return true;
        }
    }
}