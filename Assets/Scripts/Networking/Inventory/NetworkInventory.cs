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

    public bool IsEmpty => itemID == 0 || amount <= 0;
}

public class NetworkInventory : NetworkBehaviour
{
    public const int InventoryCapacity = 20;
    public const int HotbarCapacity = 5;

    [Header("Database")]
    [SerializeField] private ItemDatabase database;

    // ===== Inventory chính (20 slot) =====
    [Networked, Capacity(InventoryCapacity)]
    [OnChangedRender(nameof(OnInventoryChangedRender))]
    private NetworkArray<InventoryItem> Items => default;

    // ===== Hotbar / ToolKit (5 slot) — vẫn là dữ liệu của Player =====
    [Networked, Capacity(HotbarCapacity)]
    [OnChangedRender(nameof(OnInventoryChangedRender))]
    private NetworkArray<InventoryItem> Hotbar => default;

    public int Capacity => InventoryCapacity;
    public int HotbarSize => HotbarCapacity;
    public ItemDatabase Database => database;

    public event Action OnInventoryChanged;

    void OnInventoryChangedRender()
    {
        OnInventoryChanged?.Invoke();
    }

    public override void Spawned()
    {
        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("ItemData");
            if (database == null)
                Debug.LogError($"{name}: ItemDatabase is not assigned.");
        }
    }

    private void Update()
    {
        // Test nhanh
        if (Input.GetKeyDown(KeyCode.Q) && Object.HasStateAuthority)
        {
            TryAddItem(1, 1);
            TryAddItem(3, 1);
            TryAddItem(5, 1);
        }
    }




    #region ===== Inventory Get / Set =====

    public InventoryItem GetSlot(int index)
    {
        if (index < 0 || index >= InventoryCapacity) return default;
        return Items.Get(index);
    }

    public ItemSO GetItemData(int index)
    {
        var slot = GetSlot(index);
        if (slot.IsEmpty) return null;
        return database.GetItem(slot.itemID);
    }

    public ItemSO GetItemDataByID(int id) => database != null ? database.GetItem(id) : null;

    #endregion

    #region ===== Hotbar Get / Set =====

    public InventoryItem GetHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarCapacity) return default;
        return Hotbar.Get(index);
    }

    public ItemSO GetHotbarItemData(int index)
    {
        var slot = GetHotbarSlot(index);
        if (slot.IsEmpty) return null;
        return database.GetItem(slot.itemID);
    }

    #endregion

    #region ===== Add / Remove (Inventory chính) =====

    public bool CanAddItem(int itemID, int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return false;

        ItemSO item = database.GetItem(itemID);
        if (item == null) return false;

        int remaining = amount;
        if (item.Stackable)
        {
            for (int i = 0; i < InventoryCapacity; i++)
            {
                InventoryItem slot = Items.Get(i);
                if (slot.itemID != itemID) continue;
                remaining -= Mathf.Max(0, item.MaxStack - slot.amount);
                if (remaining <= 0) return true;
            }
        }

        int emptySlots = 0;
        for (int i = 0; i < InventoryCapacity; i++)
        {
            if (Items.Get(i).IsEmpty) emptySlots++;
        }

        int requiredSlots = item.Stackable
            ? Mathf.CeilToInt((float)remaining / item.MaxStack)
            : remaining;

        return requiredSlots <= emptySlots;
    }

    public bool TryAddItem(int itemID, int amount)
    {
        if (!CanAddItem(itemID, amount)) return false;

        ItemSO item = database.GetItem(itemID);
        int remaining = amount;

        if (item.Stackable)
        {
            for (int i = 0; i < InventoryCapacity && remaining > 0; i++)
            {
                InventoryItem slot = Items.Get(i);
                if (slot.itemID != itemID || slot.amount >= item.MaxStack) continue;

                int amountToAdd = Mathf.Min(remaining, item.MaxStack - slot.amount);
                slot.amount += amountToAdd;
                Items.Set(i, slot);
                remaining -= amountToAdd;
            }
        }

        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot < 0) return false;

            int amountToAdd = item.Stackable ? Mathf.Min(remaining, item.MaxStack) : 1;
            Items.Set(emptySlot, new InventoryItem(itemID, amountToAdd));
            remaining -= amountToAdd;
        }

        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddItem(int itemID, int amount) => TryAddItem(itemID, amount);

    public bool RemoveItem(int itemID, int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return false;
        if (!HasItem(itemID, amount)) return false;

        for (int i = 0; i < InventoryCapacity; i++)
        {
            InventoryItem slot = Items.Get(i);
            if (slot.itemID != itemID) continue;

            int amountToRemove = Mathf.Min(amount, slot.amount);
            slot.amount -= amountToRemove;
            amount -= amountToRemove;

            if (slot.amount <= 0) slot = default;
            Items.Set(i, slot);

            if (amount <= 0) return true;
        }
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RemoveItem(int itemID, int amount) => RemoveItem(itemID, amount);

    public bool HasItem(int itemID, int amount) => GetItemAmount(itemID) >= amount;

    public int GetItemAmount(int itemID)
    {
        int total = 0;
        for (int i = 0; i < InventoryCapacity; i++)
        {
            var slot = Items.Get(i);
            if (slot.itemID == itemID) total += slot.amount;
        }
        // Cũng tính cả hotbar
        for (int i = 0; i < HotbarCapacity; i++)
        {
            var slot = Hotbar.Get(i);
            if (slot.itemID == itemID) total += slot.amount;
        }
        return total;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < InventoryCapacity; i++)
            if (Items.Get(i).IsEmpty) return i;
        return -1;
    }

    public bool IsFull() => FindEmptySlot() == -1;

    #endregion

    #region ===== Di chuyển giữa Inventory ↔ Hotbar (đồng bộ Player) =====

    /// <summary>
    /// Chuyển item từ 1 slot Inventory sang 1 slot Hotbar (swap nếu đích đã có).
    /// Chỉ chạy trên StateAuthority.
    /// </summary>
    public bool MoveInventoryToHotbar(int invIndex, int hotbarIndex)
    {
        if (!Object.HasStateAuthority) return false;
        if (invIndex < 0 || invIndex >= InventoryCapacity) return false;
        if (hotbarIndex < 0 || hotbarIndex >= HotbarCapacity) return false;

        var invItem = Items.Get(invIndex);
        var hotItem = Hotbar.Get(hotbarIndex);

        // Swap
        Items.Set(invIndex, hotItem);
        Hotbar.Set(hotbarIndex, invItem);
        return true;
    }

    /// <summary>
    /// Chuyển item từ 1 slot Hotbar sang 1 slot Inventory (swap nếu đích đã có).
    /// </summary>
    public bool MoveHotbarToInventory(int hotbarIndex, int invIndex)
    {
        if (!Object.HasStateAuthority) return false;
        if (hotbarIndex < 0 || hotbarIndex >= HotbarCapacity) return false;
        if (invIndex < 0 || invIndex >= InventoryCapacity) return false;

        var hotItem = Hotbar.Get(hotbarIndex);
        var invItem = Items.Get(invIndex);

        Hotbar.Set(hotbarIndex, invItem);
        Items.Set(invIndex, hotItem);
        return true;
    }   

    /// <summary>
    /// Swap 2 slot trong Hotbar.
    /// </summary>
    public bool SwapHotbarSlots(int a, int b)
    {
        if (!Object.HasStateAuthority) return false;
        if (a < 0 || a >= HotbarCapacity || b < 0 || b >= HotbarCapacity) return false;
        if (a == b) return true;

        var itemA = Hotbar.Get(a);
        var itemB = Hotbar.Get(b);
        Hotbar.Set(a, itemB);
        Hotbar.Set(b, itemA);
        return true;
    }

    /// <summary>
    /// Swap 2 slot trong Inventory chính.
    /// </summary>
    public bool SwapInventorySlots(int a, int b)
    {
        if (!Object.HasStateAuthority) return false;
        if (a < 0 || a >= InventoryCapacity || b < 0 || b >= InventoryCapacity) return false;
        if (a == b) return true;

        var itemA = Items.Get(a);
        var itemB = Items.Get(b);
        Items.Set(a, itemB);
        Items.Set(b, itemA);
        return true;
    }

    // ===== RPC cho Client =====

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MoveInventoryToHotbar(int invIndex, int hotbarIndex)
    {
        MoveInventoryToHotbar(invIndex, hotbarIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_MoveHotbarToInventory(int hotbarIndex, int invIndex)
    {
        MoveHotbarToInventory(hotbarIndex, invIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SwapHotbarSlots(int a, int b)
    {
        SwapHotbarSlots(a, b);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SwapInventorySlots(int a, int b)
    {
        SwapInventorySlots(a, b);
    }
    #endregion
}
