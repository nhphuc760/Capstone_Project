using UnityEngine;
using Fusion;
using System;

public struct InventoryItem : INetworkStruct
{
    public int itemID;
    public int amount;


    public InventoryItem(int itemID, int amount)
    {
        this.itemID = itemID;
        this.amount = amount;
    }


    public bool IsEmpty =>
        itemID == 0 || amount <= 0;
}

public class NetworkInventory : NetworkBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    private const int capacity = 20;


    [Header("Database")]
    [SerializeField]
    private ItemDatabase database;


    [Networked, Capacity(capacity)]
    [OnChangedRender(nameof(OnChangeRender))]
    private NetworkArray<InventoryItem> Items => default;


    public int Capacity => capacity;


    public ItemDatabase Database => database;

    public event Action OnInventoryChanged;



    void OnChangeRender()
    {
        Debug.Log("Inventory Change Render");
        OnInventoryChanged?.Invoke();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("ItemData");
            if(database == null)
            {
                Debug.LogError(
                    $"{name}: ItemDatabase is not assigned."
                );
            }
        }
    }


    private void Update()
    {
        //Test
        if (Input.GetKeyDown(KeyCode.T) && HasInputAuthority)
        {
            RPC_AddItem(1, 10);
        }
        

    }

    #region Host add item
    public bool CanAddItem(int itemID, int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0)
            return false;

        ItemSO item = database.GetItem(itemID);
        if (item == null)
            return false;

        int remaining = amount;
        if (item.Stackable)
        {
            for (int i = 0; i < capacity; i++)
            {
                InventoryItem slot = Items.Get(i);
                if (slot.itemID != itemID)
                    continue;

                remaining -= Mathf.Max(0, item.MaxStack - slot.amount);
                if (remaining <= 0)
                    return true;
            }
        }

        int emptySlots = 0;
        for (int i = 0; i < capacity; i++)
        {
            if (Items.Get(i).IsEmpty)
                emptySlots++;
        }

        int requiredSlots = item.Stackable
            ? Mathf.CeilToInt((float)remaining / item.MaxStack)
            : remaining;
        return requiredSlots <= emptySlots;
    }

    public bool TryAddItem(int itemID, int amount)
    {
        if (!CanAddItem(itemID, amount))
            return false;

        ItemSO item = database.GetItem(itemID);
        int remaining = amount;

        if (item.Stackable)
        {
            for (int i = 0; i < capacity && remaining > 0; i++)
            {
                InventoryItem slot = Items.Get(i);
                if (slot.itemID != itemID || slot.amount >= item.MaxStack)
                    continue;

                int amountToAdd = Mathf.Min(remaining, item.MaxStack - slot.amount);
                slot.amount += amountToAdd;
                Items.Set(i, slot);
                remaining -= amountToAdd;
            }
        }

        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot < 0)
                return false;

            int amountToAdd = item.Stackable
                ? Mathf.Min(remaining, item.MaxStack)
                : 1;
            Items.Set(emptySlot, new InventoryItem(itemID, amountToAdd));
            remaining -= amountToAdd;
        }

        return true;
    }
    #endregion

    #region Client add item
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddItem(int itemID, int amount)
    {
        TryAddItem(itemID, amount);
    }
    #endregion

    #region Host remove item
    public bool RemoveItem(int itemID, int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;


        if (!HasItem(itemID, amount))
            return false;


        for (int i = 0; i < capacity; i++)
        {
            InventoryItem slot = Items.Get(i);

            if (slot.itemID != itemID)
                continue;


            int amountToRemove =
                Mathf.Min(amount, slot.amount);


            slot.amount -= amountToRemove;

            amount -= amountToRemove;


            if (slot.amount <= 0)
            {
                slot = default;
            }


            Items.Set(i, slot);
            if (amount <= 0)
                return true;
        }


        return true;
    }
    #endregion

    #region Client remove item
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RemoveItem(int itemID, int amount)
    {
        RemoveItem(itemID, amount);
    }
    #endregion

    #region Check if the inventory has a specific item and amount
    public bool HasItem(int itemID, int amount)
    {
        return GetItemAmount(itemID) >= amount;
    }
    #endregion

    #region Get the total amount of a specific item in the inventory
    public int GetItemAmount(int itemID)
    {
        int total = 0;

        for (int i = 0; i < capacity; i++)
        {
            InventoryItem slot = Items.Get(i);

            if (slot.itemID != itemID)
                continue;


            total += slot.amount;
        }

        return total;
    }
    #endregion

    // GET SLOT
    public InventoryItem GetSlot(int index)
    {
        if (index < 0 || index >= capacity)
            return default;


        return Items.Get(index);
    }

    // GET ITEM DATA
    public ItemSO GetItemData(int index)
    {
        InventoryItem slot = GetSlot(index);

        if (slot.IsEmpty)
            return null;


        return database.GetItem(slot.itemID);
    }

    public ItemSO GetItemDataByID(int _id)
    {
        return database.GetItem(_id);
    }

    // FIND EMPTY SLOT
    private int FindEmptySlot()
    {
        for (int i = 0; i < capacity; i++)
        {
            InventoryItem slot = Items.Get(i);

            if (slot.IsEmpty)
                return i;
        }


        return -1;
    }

    // CHECK FULL
    public bool IsFull()
    {
        return FindEmptySlot() == -1;
    }
}
