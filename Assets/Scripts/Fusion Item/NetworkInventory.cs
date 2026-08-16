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

    public const int MaxSlots = 20;

    [Networked, Capacity(MaxSlots)]
    public NetworkArray<ItemStack> Slots => default;

    public int SlotCount => MaxSlots;

    public bool IsFull =>
        FindEmptySlot() == -1;

    public bool IsEmpty =>
        ItemCount == 0;

    #region Item Count

    /// <summary>
    /// Tổng số slot đang chứa Item.
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

    #region Get Stack

    /// <summary>
    /// Lấy ItemStack trong slot.
    /// </summary>
    public ItemStack GetStack(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogError(
                $"[NetworkInventory] Invalid Slot: {slot}"
            );

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
    /// Lấy tổng số lượng của Item trong toàn bộ Inventory.
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
    /// Chỉ State Authority được phép thay đổi Inventory.
    /// </summary>
    public bool TryAddItem(int itemID, ushort amount = 1)
    {
        // ==========================================
        // AUTHORITY
        // ==========================================

        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[NetworkInventory] " +
                "Only State Authority can modify Inventory."
            );

            return false;
        }

        if (amount <= 0)
            return false;

        // ==========================================
        // CHECK ITEM
        // ==========================================

        ObjectData data = Database.GetItem(itemID);

        if (data == null)
        {
            Debug.LogError(
                $"[NetworkInventory] ItemID {itemID} không tồn tại."
            );

            return false;
        }

        int remaining = amount;

        // ==========================================
        // STACK VÀO SLOT ĐÃ CÓ ITEM
        // ==========================================

        if (data.stackable)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                ItemStack stack = Slots.Get(i);

                if (stack.ItemID != itemID)
                    continue;

                if (stack.Amount >= data.maxStack)
                    continue;

                int space =
                    data.maxStack - stack.Amount;

                int add =
                    Mathf.Min(
                        remaining,
                        space
                    );

                stack.Amount += (ushort)add;

                Slots.Set(i, stack);

                remaining -= add;

                if (remaining <= 0)
                    return true;
            }
        }

        // ==========================================
        // TẠO SLOT MỚI
        // ==========================================

        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();

            if (emptySlot == -1)
            {
                Debug.Log(
                    "[NetworkInventory] Inventory Full."
                );

                return false;
            }

            int add;

            if (data.stackable)
            {
                add = Mathf.Min(
                    remaining,
                    data.maxStack
                );
            }
            else
            {
                add = 1;
            }

            ItemStack newStack = new ItemStack
            {
                ItemID = itemID,
                Amount = (ushort)add,
                Durability = ushort.MaxValue
            };

            Slots.Set(
                emptySlot,
                newStack
            );

            remaining -= add;
        }

        return true;
    }

    #endregion

    #region Remove

    /// <summary>
    /// Xóa một lượng Item khỏi slot.
    /// </summary>
    public bool TryRemoveItem(
        int slot,
        ushort amount = 1)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (!IsValidSlot(slot))
            return false;

        if (amount <= 0)
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

    /// <summary>
    /// Tìm Slot trống đầu tiên.
    /// </summary>
    public int FindEmptySlot()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots.Get(i).ItemID == 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Kiểm tra Inventory có Item hay không.
    /// </summary>
    public bool HasItem(int itemID)
    {
        return FindSlot(itemID) != -1;
    }

    #endregion

    #region Clear Inventory

    public void ClearInventory()
    {
        if (!Object.HasStateAuthority)
            return;

        for (int i = 0; i < MaxSlots; i++)
        {
            Slots.Set(i, default);
        }
    }

    #endregion

    #region Utility

    private bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < MaxSlots;
    }

    #endregion

    #region Debug

    [ContextMenu("Debug Inventory")]
    private void DebugInventory()
    {
        Debug.Log(
            $"========== INVENTORY ==========\n" +
            $"Items: {ItemCount}"
        );

        for (int i = 0; i < MaxSlots; i++)
        {
            ItemStack stack = Slots.Get(i);

            if (stack.ItemID == 0)
            {
                Debug.Log(
                    $"Slot {i}: EMPTY"
                );

                continue;
            }

            ObjectData data =
                Database.GetItem(stack.ItemID);

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

    #endregion
}