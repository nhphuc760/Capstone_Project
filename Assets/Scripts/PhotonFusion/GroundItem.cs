using Fusion;
using UnityEngine;

/// <summary>
/// Đại diện cho một vật phẩm trên mặt đất.
/// Chỉ đồng bộ ItemID và Amount.
/// Các dữ liệu khác sẽ được lấy từ ItemDatabase.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class GroundItem : FusionBehaviour
{
    #region Network
    [Networked]
    public int ItemID { get; set; }

    [Networked]
    public ushort Amount { get; set; } = 1;
    #endregion

    #region Data
    /// <summary>
    /// ObjectData tương ứng.
    /// </summary>
    public ObjectData Data => Database.GetItem(ItemID);

    public string Name => Data.objectName;

    public Sprite Icon => Data.icon;

    public GameObject Prefab => Data.objectPrefab;

    public ObjectType Type => Data.objectType;

    public ObjectEffectData Effect => Data.effect;

    //public float Weight => Data.weight;

    public int Points => Data.objectPoints;

    public bool IsRare => Data.isRare;

    public bool IsStackable => Data.stackable;

    public int MaxStack => Data.maxStack;
    #endregion

    #region Initialize
    /// <summary>
    /// Chỉ Host (State Authority) được phép khởi tạo GroundItem.
    /// </summary>
    public void Initialize(int itemID, ushort amount = 1)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[GroundItem] Only State Authority can initialize.");
            return;
        }

        ItemID = itemID;
        Amount = amount;
    }
    #endregion

    #region Pickup
    /// <summary>
    /// Thử nhặt vật phẩm.
    /// Hiện tại chỉ thêm vào Inventory.
    /// Việc gọi hàm này sẽ được thực hiện bởi Host sau khi xử lý RPC.
    /// </summary>
    public bool TryPickup(NetworkInventory inventory)
    {
        if (inventory == null)
            return false;

        bool success = inventory.TryAddItem(ItemID, Amount);

        if (!success)
            return false;

        Runner.Despawn(Object);

        return true;
    }
    #endregion

    #region Debug
    [ContextMenu("Print Info")]
    private void PrintInfo()
    {
        Debug.Log(
            $"Item : {Name}\n" +
            $"ID : {ItemID}\n" +
            $"Amount : {Amount}\n" +
            //$"Weight : {Weight}\n" +
            $"Points : {Points}");
    }
    #endregion
}