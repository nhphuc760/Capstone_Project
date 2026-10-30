
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableShopSO", menuName = "Shop/Consumable Shop")]
public class ConsumableShopSO : ScriptableObject
{
    [Serializable]
    public class ConsumableShopItem
    {
        [Header("Product")]
        public ItemSO itemSO;

        [Header("Prices")]
        [Min(1)] public int buyPrice = 100;
        [Min(1)] public int sellPrice = 50;
    }

    [Header("Consumable Products")]
    [SerializeField] private List<ConsumableShopItem> shopItems = new();

    public IReadOnlyList<ConsumableShopItem> ShopItems => shopItems;

    public bool TryGetItem(int itemID, out ConsumableShopItem result)
    {
        result = null;

        foreach (ConsumableShopItem shopItem in shopItems)
        {
            if (shopItem == null || shopItem.itemSO == null)
                continue;

            if (shopItem.itemSO.ItemId != itemID)
                continue;

            if (shopItem.itemSO.itemType != ItemType.Consumable)
                return false;

            if (shopItem.buyPrice <= 0 || shopItem.sellPrice <= 0)
                return false;

            result = shopItem;
            return true;
        }

        return false;
    }
}
