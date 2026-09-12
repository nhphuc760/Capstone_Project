using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Online Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    [SerializeField]
    private List<ItemSO> items = new();

    private Dictionary<int, ItemSO> itemLookup;


    [Title("Make sure dataSO inside Resources Folder")]
    [Button("Auto Find ItemDataSO")]
    public void BakeData()
    {
       items = Resources.LoadAll<ItemSO>("").ToList();
        Initialize();
    }


    #region Lookup in ItemSO by itemId
    public void Initialize()
    {
        if(itemLookup != null)
        {
            itemLookup.Clear();
            itemLookup = null;
        }
        itemLookup = new Dictionary<int, ItemSO>();

        foreach (ItemSO item in items)
        {
            if (item == null)
                continue;

            if (itemLookup.ContainsKey(item.ItemId))
            {
                Debug.LogError(
                    $"Duplicate Item ID detected: {item.ItemId} for item {item.ItemName}. Each item must have a unique ID."
                );

                continue;
            }

            itemLookup.Add(item.ItemId, item);
        }
    }
    #endregion

    #region Get item by itemId
    public ItemSO GetItem(int itemID)
    {
        if (itemLookup == null)
            Initialize();

        if (itemLookup.TryGetValue(itemID, out ItemSO item))
            return item;

        Debug.LogWarning($"Item ID {itemID} not found in ItemDatabase.");

        return null;
    }

    
    public bool Contains(int itemID)
    {
        if (itemLookup == null)
            Initialize();

        return itemLookup.ContainsKey(itemID);
    }
    #endregion
}