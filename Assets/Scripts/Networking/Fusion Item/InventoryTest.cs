using Fusion;
using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [SerializeField]
    private NetworkInventory inventory;

    private void Awake()
    {
        Debug.Log("InventoryTest Awake");
    }

    private void Update()
    {
        if (inventory == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestAddPotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestAddBomb();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestRemovePotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestCheckPotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PrintInventory();
        }
    }

    private void TestAddPotion()
{
    if (inventory.Object.HasStateAuthority)
    {
        bool result = inventory.AddItem(1, 3);

        Debug.Log($"Add Potion x3: {result}");
    }
    else
    {
        inventory.RPC_AddItem(1, 3);

        Debug.Log("Requested Host to add Potion x3");
    }
}

    private void TestAddBomb()
    {
        if (inventory.Object.HasStateAuthority)
        {
            bool result = inventory.AddItem(2, 2);
            Debug.Log($"Add Bomb x2: {result}");
        }
        else
        {
            inventory.RPC_AddItem(2, 2);
            Debug.Log("Requested Host to add Bomb x2");
        }
    }

    private void TestRemovePotion()
    {
        if (inventory.Object.HasStateAuthority)
        {
            bool result = inventory.RemoveItem(1, 2);
            Debug.Log($"Remove Potion x2: {result}");
        }
        else
        {
            inventory.RPC_RemoveItem(1, 2);
            Debug.Log("Requested Host to remove Potion x2");
        }
    }

    private void TestCheckPotion()
    {
        int amount = inventory.GetItemAmount(1);
        Debug.Log($"Potion amount: {amount}");

        int bombAmount = inventory.GetItemAmount(2);
        Debug.Log($"Bomb amount: {bombAmount}");       
    }

    private void PrintInventory()
    {
        for (int i = 0; i < inventory.Capacity; i++)
        {
            InventoryItem slot = inventory.GetSlot(i);

            if (slot.IsEmpty)
            {
                Debug.Log($"Slot {i}: Empty");
                continue;
            }

            ItemSO data = inventory.Database.GetItem(slot.itemID);

            if (data == null)
            {
                Debug.LogError(
                    $"Slot {i}: Cannot find ItemSO for ID {slot.itemID}"
                );
                continue;
            }

            Debug.Log(
                $"Slot {i}: " +
                $"{data.ItemName} " +
                $"x{slot.amount} " +
                $"(ID: {slot.itemID})"
            );
        }
    }
}