using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [Header("Test Amount")]
    [SerializeField]
    private ushort amountPerPress = 1;

    [Header("Keyboard Item IDs")]
    [SerializeField]
    private int itemID1 = 1;

    [SerializeField]
    private int itemID2 = 2;

    [SerializeField]
    private int itemID3 = 3;

    [SerializeField]
    private int itemID4 = 4;

    [SerializeField]
    private int itemID5 = 5;

    private NetworkInventory Inventory =>
        NetworkItem.Local?.Inventory;

    private void Update()
    {
        NetworkInventory inventory = Inventory;

        // Player chưa được Spawn
        if (inventory == null)
            return;

        // Chỉ State Authority được thay đổi Inventory
        if (!inventory.Object.HasStateAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            AddItem(inventory, itemID1);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddItem(inventory, itemID2);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AddItem(inventory, itemID3);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AddItem(inventory, itemID4);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            AddItem(inventory, itemID5);

        // Space = xem Inventory
        if (Input.GetKeyDown(KeyCode.Space))
            PrintInventory(inventory);

        // C = Clear Inventory
        if (Input.GetKeyDown(KeyCode.C))
        {
            inventory.ClearInventory();

            Debug.Log(
                "[InventoryTest] Inventory Cleared."
            );
        }
    }

    private void AddItem(
        NetworkInventory inventory,
        int itemID)
    {
        bool success =
            inventory.TryAddItem(
                itemID,
                amountPerPress
            );

        if (success)
        {
            Debug.Log(
                $"[InventoryTest] " +
                $"Added ItemID {itemID} " +
                $"x{amountPerPress}"
            );

            PrintInventory(inventory);
        }
        else
        {
            Debug.LogWarning(
                $"[InventoryTest] " +
                $"Cannot add ItemID {itemID}"
            );
        }
    }

    private void PrintInventory(
        NetworkInventory inventory)
    {
        Debug.Log(
            "========== INVENTORY =========="
        );

        for (
            int i = 0;
            i < NetworkInventory.MaxSlots;
            i++)
        {
            ItemStack stack =
                inventory.Slots.Get(i);

            if (stack.ItemID == 0)
            {
                Debug.Log(
                    $"Slot {i}: EMPTY"
                );

                continue;
            }

            ObjectData data =
                FusionItemManager.Instance
                    .ItemDatabase
                    .GetItem(stack.ItemID);

            string itemName =
                data != null
                    ? data.objectName
                    : "UNKNOWN";

            Debug.Log(
                $"Slot {i}: " +
                $"{itemName} " +
                $"(ID: {stack.ItemID}) " +
                $"x{stack.Amount}"
            );
        }

        Debug.Log(
            "==============================="
        );
    }
}