using Fusion;
using UnityEngine;

/// <summary>
/// Inventory được đồng bộ bởi Photon Fusion.
/// Chỉ đồng bộ ItemStack, không đồng bộ ObjectData.
/// </summary>
public class NetworkInventory : FusionBehaviour
{
    public static NetworkInventory Local =>
    NetworkPlayer.Local?.Inventory;

    public bool HasStateAuthority =>
    Object.HasStateAuthority; //Host kiểm tra xem ai là người pick-up đồ, người này có gần đồ ko hay đang bấm cho việc khác

    public bool HasInputAuthority =>
    Object.HasInputAuthority; //Kiểm tra input của người chơi

    private ItemDatabase Database =>
    FusionManager.Instance.ItemDatabase;

    public const int MaxSlots = 20;

    [Networked, Capacity(MaxSlots)]
    public NetworkArray<ItemStack> Slots => default;

    /// <summary>
    /// Số slot tối đa.
    /// </summary>
    public int SlotCount => MaxSlots;

    /// <summary>
    /// Inventory hiện tại có đầy không.
    /// </summary>
    public bool IsFull => FindEmptySlot() == -1;

    /// <summary>
    /// Inventory rỗng.
    /// </summary>
    public bool IsEmpty => ItemCount == 0;

    #region Item count
    /// <summary>
    /// Tổng số Slot đang chứa Item.
    /// </summary>
    public int ItemCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < MaxSlots; i++)
            {
                if (Slots.Get(i).ItemID != 0)
                    count++;
            }

            return count;
        }
    }
    #endregion

    #region Get stack
    /// <summary>
    /// Lấy ItemStack trong slot.
    /// </summary>
    public ItemStack GetStack(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogError($"[Inventory] Invalid Slot : {slot}");
            return default;
        }

        return Slots.Get(slot);
    }
    #endregion

    #region Get Item
    /// <summary>
    /// Lấy InventoryItem để đọc ObjectData.
    /// </summary>
    public InventoryItem GetItem(int slot)
    {
        return new InventoryItem(GetStack(slot));
    }
    #endregion

    #region Get Amount
    /// <summary>
    /// Lấy tổng số lượng của Item.
    /// </summary>
    public ushort GetAmount(int itemID)
    {
        ushort amount = 0;

        for (int i = 0; i < MaxSlots; i++)
        {
            ItemStack stack = Slots.Get(i);

            if (stack.ItemID == itemID)
                amount += stack.Amount;
        }

        return amount;
    }
    #endregion

    #region Add Item
    /// <summary>
    /// Thêm Item vào Inventory.
    /// </summary>
    public bool TryAddItem(int itemID, ushort amount = 1)
    {
        #region Check ItemID
        ObjectData data =
            FusionManager.Instance.ItemDatabase.GetItem(itemID);

        if (data == null)
        {
            Debug.LogError($"ItemID {itemID} không tồn tại.");
            return false;
        }
        #endregion
        //------------------------------------------------
        // Nếu Item Stack được
        //------------------------------------------------
        #region Stackable
        if (data.stackable)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                ItemStack stack = Slots.Get(i);

                if (stack.ItemID != itemID)
                    continue;

                if (stack.Amount >= data.maxStack)
                    continue;

                ushort add =
                    (ushort)Mathf.Min(
                        amount,
                        data.maxStack - stack.Amount);

                stack.Amount += add;

                Slots.Set(i, stack);

                return true;
            }
        }
        #endregion
        //------------------------------------------------
        // Tìm Slot trống
        //------------------------------------------------
        #region Find Empty Slot
        for (int i = 0; i < MaxSlots; i++)
        {
            ItemStack stack = Slots.Get(i);

            if (stack.ItemID != 0)
                continue;

            stack.ItemID = itemID;
            stack.Amount = amount;
            stack.Durability = ushort.MaxValue;

            Slots.Set(i, stack);

            return true;     
        }

        Debug.Log("Inventory Full");

        return false;
        #endregion
    }
    #endregion

    #region Remove
    public bool TryRemoveItem(int slot, ushort amount = 1)
    {
        if (!IsValidSlot(slot))
            return false;

        ItemStack stack = Slots.Get(slot);

        if (stack.ItemID == 0)
            return false;

        if (stack.Amount > amount)
        {
            stack.Amount -= amount;
        }
        else
        {
            stack = default;
        }

        Slots.Set(slot, stack);

        return true;
    }
    #endregion

    #region Find
    /// <summary>
    /// Tìm Slot đầu tiên chứa Item.
    /// </summary>
    public int FindSlot(int itemID)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots.Get(i).ItemID == itemID)
                return i;
        }

        return -1;
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots.Get(i).ItemID == 0)
                return i;
        }

        return -1;
    }

    public bool HasItem(int itemID)
    {
        return FindSlot(itemID) != -1;
    }
    #endregion

    #region Clear Inventory
    public void ClearInventory()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            Slots.Set(i, default);
        }
    }
    #endregion

    #region Utility
    // public float GetCurrentWeight()
    // {
    //     float weight = 0;

    //     for (int i = 0; i < MaxSlots; i++)
    //     {
    //         ItemStack stack = Slots.Get(i);

    //         if (stack.ItemID == 0)
    //             continue;

    //         ObjectData data =
    //             FusionManager.Instance.ItemDatabase.GetItem(stack.ItemID);

    //         //weight += data.weight * stack.Amount;
    //     }

    //     return weight;
    // }

    public int GetCurrentPoints()
    {
        int point = 0;

        for (int i = 0; i < MaxSlots; i++)
        {
            ItemStack stack = Slots.Get(i);

            if (stack.ItemID == 0)
                continue;

            ObjectData data =
                FusionManager.Instance.ItemDatabase.GetItem(stack.ItemID);

            point += data.objectPoints * stack.Amount;
        }

        return point;
    }

    private bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < MaxSlots;
    }
    #endregion

    [ContextMenu("Debug Inventory")]
    private void DebugInventory()
    {
        Debug.Log($"Items : {ItemCount}");

        // Debug.Log($"Weight : {GetCurrentWeight()}");

        Debug.Log($"Points : {GetCurrentPoints()}");
    }

    /*
    * ==========================================================
    * NETWORK INVENTORY
    * ----------------------------------------------------------
    * Chỉ lưu dữ liệu Runtime.
    *
    * KHÔNG lưu:
    *  - ObjectData
    *  - Sprite
    *  - Prefab
    *  - ObjectEffectData
    *
    * Chỉ lưu:
    *  - ItemID
    *  - Amount
    *  - Durability
    *
    * Muốn lấy ObjectData:
    * Database.GetItem(ItemID)
    * ==========================================================
    */
}