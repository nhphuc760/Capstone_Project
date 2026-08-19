using UnityEngine;
using Fusion;

public class InventoryTest : MonoBehaviour
{
    private NetworkInventory personalInventory;
    private SharedInventory sharedInventory;
    private InventoryMode inventoryManager;
    private NetworkObject playerObject;

    private void Awake()
    {
        personalInventory =
            GetComponent<NetworkInventory>();

        playerObject =
            GetComponent<NetworkObject>();

        inventoryManager =
            FindFirstObjectByType<InventoryMode>();

        if (inventoryManager != null)
        {
            sharedInventory =
                inventoryManager.SharedInventory;
        }
    }

    private void Update()
    {
        if (playerObject == null)
                Debug.Log(
        $"[{playerObject.InputAuthority}] " +
        $"InputAuthority={playerObject.HasInputAuthority} | " +
        $"StateAuthority={playerObject.HasStateAuthority}"
    );

        if (inventoryManager == null)
            return;

        // Chỉ Player sở hữu object mới nhận input
        if (!playerObject.HasInputAuthority)
            return;

        // DEBUG INPUT CLIENT
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 1"
            );

            TestAddPotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 2"
            );

            TestAddBomb();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 3"
            );

            TestRemovePotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 4"
            );

            TestCheckInventory();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 5"
            );

            PrintInventory();
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log(
                $"[INPUT] Player {playerObject.InputAuthority} " +
                $"pressed 6"
            );

            SplitInventory();
        }
    }

    // =====================================================
    // ADD POTION
    // =====================================================

    private void TestAddPotion()
    {
        if (inventoryManager.mode == Mode.SharedInv)
        {
            if (sharedInventory == null)
                return;

            PlayerRef owner =
                playerObject.InputAuthority;

            if (sharedInventory.Object.HasStateAuthority)
            {
                bool result =
                    sharedInventory.AddItem(
                        1,
                        3,
                        owner
                    );

                Debug.Log(
                    $"[HOST] Potion x3 → {owner} | {result}"
                );
            }
            else
            {
                sharedInventory.RPC_AddItem(
                    1,
                    3,
                    owner
                );
            }

            return;
        }

        // PERSONAL
        if (personalInventory == null)
            return;

        if (personalInventory.Object.HasStateAuthority)
        {
            bool result =
                personalInventory.AddItem(1, 3);

            Debug.Log(
                $"[HOST] Personal Potion x3 → {result}"
            );
        }
        else
        {
            personalInventory.RPC_AddItem(1, 3);
        }
    }

    // =====================================================
    // ADD BOMB
    // =====================================================

    private void TestAddBomb()
    {
        if (inventoryManager.mode == Mode.SharedInv)
        {
            if (sharedInventory == null)
                return;

            PlayerRef owner =
                playerObject.InputAuthority;

            if (sharedInventory.Object.HasStateAuthority)
            {
                bool result =
                    sharedInventory.AddItem(
                        2,
                        2,
                        owner
                    );

                Debug.Log(
                    $"[HOST] Bomb x2 → {owner} | {result}"
                );
            }
            else
            {
                sharedInventory.RPC_AddItem(
                    2,
                    2,
                    owner
                );
            }

            return;
        }

        // PERSONAL
        if (personalInventory == null)
            return;

        if (personalInventory.Object.HasStateAuthority)
        {
            bool result =
                personalInventory.AddItem(2, 2);

            Debug.Log(
                $"[HOST] Personal Bomb x2 → {result}"
            );
        }
        else
        {
            personalInventory.RPC_AddItem(2, 2);
        }
    }

    // =====================================================
    // REMOVE POTION
    // =====================================================

    private void TestRemovePotion()
    {
        if (inventoryManager.mode == Mode.SharedInv)
        {
            Debug.Log(
                "[INPUT] Remove Shared Potion chưa implement."
            );

            return;
        }

        if (personalInventory == null)
            return;

        if (personalInventory.Object.HasStateAuthority)
        {
            bool result =
                personalInventory.RemoveItem(1, 2);

            Debug.Log(
                $"[HOST] Remove Personal Potion x2 → {result}"
            );
        }
        else
        {
            personalInventory.RPC_RemoveItem(1, 2);
        }
    }

    // =====================================================
    // CHECK
    // =====================================================

    private void TestCheckInventory()
    {
        if (inventoryManager.mode == Mode.SharedInv)
        {
            int potion = 0;
            int bomb = 0;

            for (int i = 0;
                 i < sharedInventory.Capacity;
                 i++)
            {
                SharedInventoryItem slot =
                    sharedInventory.GetSlot(i);

                if (slot.IsEmpty)
                    continue;

                if (slot.itemID == 1)
                    potion += slot.amount;

                if (slot.itemID == 2)
                    bomb += slot.amount;
            }

            Debug.Log(
                $"[SHARED] Potion={potion} | Bomb={bomb}"
            );

            return;
        }

        int personalPotion =
            personalInventory.GetItemAmount(1);

        int personalBomb =
            personalInventory.GetItemAmount(2);

        Debug.Log(
            $"[PERSONAL] " +
            $"Potion={personalPotion} | " +
            $"Bomb={personalBomb}"
        );
    }

    // =====================================================
    // PRINT
    // =====================================================

    private void PrintInventory()
    {
        if (inventoryManager.mode == Mode.SharedInv)
        {
            Debug.Log("[SHARED INVENTORY]");

            for (int i = 0;
                 i < sharedInventory.Capacity;
                 i++)
            {
                SharedInventoryItem slot =
                    sharedInventory.GetSlot(i);

                if (slot.IsEmpty)
                    continue;

                ItemSO data =
                    sharedInventory.Database.GetItem(
                        slot.itemID
                    );

                if (data == null)
                    continue;

                Debug.Log(
                    $"Slot {i}: " +
                    $"{data.ItemName} x{slot.amount} " +
                    $"Owner={slot.owner}"
                );
            }

            return;
        }

        Debug.Log("[PERSONAL INVENTORY]");

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
                $"Slot {i}: " +
                $"{data.ItemName} x{slot.amount}"
            );
        }
    }

    // =====================================================
    // SPLIT
    // =====================================================

    private void SplitInventory()
    {
        if (inventoryManager.mode != Mode.SharedInv)
        {
            Debug.Log(
                "[INPUT] Inventory already Personal."
            );

            return;
        }

        inventoryManager.RPC_SplitInventory();
    }
}