using Fusion;
using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    private NetworkInventory personalInventory;
    private NetworkObject playerObject;

    private void Awake()
    {
        personalInventory =
            GetComponent<NetworkInventory>();

        playerObject =
            GetComponent<NetworkObject>();
    }

    private void Update()
    {
        if (playerObject == null)
            return;

        if (!playerObject.HasInputAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} pressed 1"
            );

            TestAddPotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} pressed 2"
            );

            TestAddBomb();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} pressed 3"
            );

            TestRemovePotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} pressed 4"
            );

            TestCheckInventory();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} pressed 5"
            );

            PrintInventory();
        }
    }

    private void TestAddPotion()
    {
        AddPersonalItem(1, 3, "Potion");
    }

    private void TestAddBomb()
    {
        AddPersonalItem(2, 2, "Bomb");
    }

    private void AddPersonalItem(
        int itemID,
        int amount,
        string itemName)
    {
        if (personalInventory == null)
            return;

        if (personalInventory.Object.HasStateAuthority)
        {
            bool result =
                personalInventory.AddItem(itemID, amount);

            Debug.Log(
                $"[HOST] Personal {itemName} x{amount} -> {result}"
            );
        }
        else
        {
            personalInventory.RPC_AddItem(itemID, amount);
        }
    }

    private void TestRemovePotion()
    {
        if (personalInventory == null)
            return;

        if (personalInventory.Object.HasStateAuthority)
        {
            bool result =
                personalInventory.RemoveItem(1, 2);

            Debug.Log(
                $"[HOST] Remove Personal Potion x2 -> {result}"
            );
        }
        else
        {
            personalInventory.RPC_RemoveItem(1, 2);
        }
    }

    private void TestCheckInventory()
    {
        if (personalInventory == null)
            return;

        int personalPotion =
            personalInventory.GetItemAmount(1);

        int personalBomb =
            personalInventory.GetItemAmount(2);

        Debug.Log(
            $"[PERSONAL] Potion={personalPotion} | Bomb={personalBomb}"
        );
    }

    private void PrintInventory()
    {
        if (personalInventory == null)
            return;

        Debug.Log(
            $"[PERSONAL INVENTORY] Player {playerObject.InputAuthority}"
        );

        for (int i = 0;
             i < personalInventory.Capacity;
             i++)
        {
            InventoryItem slot =
                personalInventory.GetSlot(i);

            if (slot.IsEmpty)
                continue;

            ItemSO data =
                personalInventory.Database.GetItem(
                    slot.itemID
                );

            if (data == null)
                continue;

            Debug.Log(
                $"Slot {i}: {data.ItemName} x{slot.amount}"
            );
        }
    }
}
