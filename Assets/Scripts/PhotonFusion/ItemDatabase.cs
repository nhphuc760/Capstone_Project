using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    [SerializeField]
    private List<ObjectData> items = new();

    private Dictionary<int, ObjectData> itemLookup;

    /// <summary>
    /// Khởi tạo Dictionary để tra cứu nhanh theo ItemID.
    /// Chỉ cần gọi 1 lần khi game bắt đầu.
    /// </summary>
    public void Initialize()
    {
        itemLookup = new Dictionary<int, ObjectData>();

        foreach (ObjectData item in items)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemDatabase] Có một ObjectData bị null.");
                continue;
            }

            if (itemLookup.ContainsKey(item.itemID))
            {
                Debug.LogError($"[ItemDatabase] Trùng ItemID: {item.itemID} ({item.objectName})");
                continue;
            }

            itemLookup.Add(item.itemID, item);
        }

        Debug.Log($"[ItemDatabase] Loaded {itemLookup.Count} items.");
    }

    /// <summary>
    /// Lấy ObjectData theo ItemID.
    /// </summary>
    public ObjectData GetItem(int itemID)
    {
        if (itemLookup == null)
            Initialize();

        if (itemLookup.TryGetValue(itemID, out ObjectData item))
            return item;

        Debug.LogWarning($"[ItemDatabase] Không tìm thấy ItemID: {itemID}");
        return null;
    }

    /// <summary>
    /// Kiểm tra ItemID có tồn tại không.
    /// </summary>
    public bool HasItem(int itemID)
    {
        if (itemLookup == null)
            Initialize();
        Debug.Log("Item already loaded: " + itemLookup.Count);
        Debug.Log("Đã có itemID: " + itemID + " ? " + itemLookup.ContainsKey(itemID));

        return itemLookup.ContainsKey(itemID);
    }

    /// <summary>
    /// Trả về toàn bộ danh sách item (Read Only).
    /// </summary>
    public IReadOnlyList<ObjectData> GetAllItems()
    {
        return items;
    }
}