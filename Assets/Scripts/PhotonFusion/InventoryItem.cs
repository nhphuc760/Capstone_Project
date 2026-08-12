using UnityEngine;

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
public readonly struct InventoryItem
{
    /// <summary>
    /// Dữ liệu được đồng bộ bởi Fusion.
    /// </summary>
    public readonly ItemStack Stack;

    public InventoryItem(ItemStack stack)
    {
        Stack = stack;
    }

    /// <summary>
    /// ObjectData tương ứng với ItemID.
    /// </summary>
    public ObjectData Data =>
        FusionManager.Instance.ItemDatabase.GetItem(Stack.ItemID);

    /// <summary>
    /// ItemID của Item.
    /// </summary>
    public int ItemID => Stack.ItemID;

    /// <summary>
    /// Số lượng.
    /// </summary>
    public ushort Amount => Stack.Amount;

    /// <summary>
    /// Độ bền.
    /// </summary>
    public ushort Durability => Stack.Durability;

    /// <summary>
    /// Tên Item.
    /// </summary>
    public string Name => Data.objectName;

    /// <summary>
    /// Icon.
    /// </summary>
    public Sprite Icon => Data.icon;

    /// <summary>
    /// Prefab ngoài map.
    /// </summary>
    public GameObject Prefab => Data.objectPrefab;

    /// <summary>
    /// Điểm.
    /// </summary>
    public int Points => Data.objectPoints;

    /// <summary>
    /// Khối lượng.
    /// </summary>
    //public float Weight => Data.weight;

    /// <summary>
    /// Loại Item.
    /// </summary>
    public ObjectType Type => Data.objectType;

    /// <summary>
    /// Effect của Item.
    /// </summary>
    public ObjectEffectData Effect => Data.effect;

    /// <summary>
    /// Slot rỗng.
    /// </summary>
    public bool IsEmpty => Stack.ItemID == 0;

    /// <summary>
    /// Item hiếm.
    /// </summary>
    public bool IsRare => Data.isRare;
}