using Fusion;
using UnityEngine;

public class SharedInventory : NetworkBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    private const int capacity = 20;

    [Header("Database")]
    [SerializeField]
    private ItemDatabase database;

    [Networked, Capacity(capacity)]
    private NetworkArray<SharedInventoryItem> Items => default;

    public int Capacity => capacity;

    public ItemDatabase Database => database;

    public override void Spawned()
    {
        base.Spawned();

        if (database == null)
        {
            Debug.LogError(
                $"{name}: ItemDatabase is not assigned."
            );
        }
    }

    // =====================================================
    // ADD ITEM TO SHARED INVENTORY
    // =====================================================

    public bool AddItem(
        int itemID,
        int amount,
        PlayerRef owner)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        ItemSO item =
            database.GetItem(itemID);

        if (item == null)
            return false;

        // ---------------------------------------------
        // STACKABLE
        // ---------------------------------------------

        if (item.Stackable)
        {
            for (int i = 0; i < capacity; i++)
            {
                SharedInventoryItem slot =
                    Items.Get(i);

                // Chỉ stack nếu cùng item + cùng owner
                if (slot.itemID != itemID)
                    continue;

                if (slot.owner != owner)
                    continue;

                if (slot.amount >= item.MaxStack)
                    continue;

                int availableSpace =
                    item.MaxStack - slot.amount;

                int amountToAdd =
                    Mathf.Min(
                        amount,
                        availableSpace
                    );

                slot.amount += amountToAdd;

                Items.Set(i, slot);

                amount -= amountToAdd;

                if (amount <= 0)
                    return true;
            }
        }

        // ---------------------------------------------
        // CREATE NEW SLOT
        // ---------------------------------------------

        while (amount > 0)
        {
            int emptySlot =
                FindEmptySlot();

            if (emptySlot == -1)
                return false;

            int amountToAdd;

            if (item.Stackable)
            {
                amountToAdd =
                    Mathf.Min(
                        amount,
                        item.MaxStack
                    );
            }
            else
            {
                amountToAdd = 1;
            }

            Items.Set(
                emptySlot,
                new SharedInventoryItem(
                    itemID,
                    amountToAdd,
                    owner
                )
            );

            amount -= amountToAdd;
        }

        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddItem(int itemID, int amount, PlayerRef owner)
    {
        AddItem(itemID, amount, owner);
    }

    // =====================================================
    // GET SLOT
    // =====================================================

    public SharedInventoryItem GetSlot(int index)
    {
        if (index < 0 || index >= capacity)
            return default;

        return Items.Get(index);
    }

    // =====================================================
    // REMOVE SLOT
    // =====================================================

    public void ClearSlot(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        if (index < 0 || index >= capacity)
            return;

        Items.Set(
            index,
            default
        );
    }

    // =====================================================
    // FIND EMPTY SLOT
    // =====================================================

    private int FindEmptySlot()
    {
        for (int i = 0; i < capacity; i++)
        {
            SharedInventoryItem slot =
                Items.Get(i);

            if (slot.IsEmpty)
                return i;
        }

        return -1;
    }

    // =====================================================
    // CLEAR ALL
    // =====================================================

    public void Clear()
    {
        if (!Object.HasStateAuthority)
            return;

        for (int i = 0; i < capacity; i++)
        {
            Items.Set(
                i,
                default
            );
        }
    }
}