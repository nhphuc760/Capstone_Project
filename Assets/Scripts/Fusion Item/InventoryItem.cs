using UnityEngine;

#region Instructions
/// <summary>
/// Wrapper giúp truy cập ObjectData từ ItemStack một cách thuận tiện.
/// 
/// LƯU Ý:
/// - Không phải Network Object.
/// - Không được lưu trong NetworkArray.
/// - Không được Serialize.
/// - Chỉ dùng để đọc dữ liệu khi cần (UI, Tooltip, Gameplay...).
/// 
/// Fusion chỉ đồng bộ ItemStack.
/// InventoryItem chỉ là lớp "đọc" dữ liệu.
/// </summary>
#endregion

public readonly struct InventoryItem
{
    public readonly ItemStack Stack; // Dữ liệu được đồng bộ bởi Fusion.

    public InventoryItem(ItemStack stack)
    {
        Stack = stack;
    }

    public ObjectData Data =>
        FusionItemManager.Instance.ItemDatabase.GetItem(Stack.ItemID); // ObjectData tương ứng với ItemID.

    public int ItemID => Stack.ItemID; // ItemID của Item.

    public ushort Amount => Stack.Amount; // Số lượng.

    public ushort Durability => Stack.Durability; // Độ bền.

    public string Name => Data.objectName; // Tên Item.

    public Sprite Icon => Data.icon; // Icon.

    public GameObject Prefab => Data.objectPrefab; // Prefab ngoài map.

    public ObjectType Type => Data.objectType; // Loại Item.

    public ObjectEffectData Effect => Data.effect; // Effect của Item.

    public bool IsEmpty => Stack.ItemID == 0; // Slot rỗng.

    public bool IsRare => Data.isRare; // Item hiếm.
}