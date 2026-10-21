
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopSO", menuName = "Shop/Shop Data")]
public class ShopSO : ScriptableObject
{
    [Serializable]
    public class ShopItem
    {
        [Header("Product")]
        public ItemSO itemSO;

        [Header("Prices")]
        [Min(1)] public int buyPrice = 100;
        [Min(1)] public int sellPrice = 50;
    }

    [Header("Shop Products")]
    [SerializeField] private List<ShopItem> shopItems = new();

    public IReadOnlyList<ShopItem> ShopItems => shopItems;

    public bool TryGetItem(int itemID, out ShopItem result)
    {
        result = null;

        foreach (ShopItem shopItem in shopItems)
        {
            if (shopItem == null || shopItem.itemSO == null)
                continue;

            if (shopItem.itemSO.ItemId != itemID)
                continue;

            if (shopItem.buyPrice <= 0 || shopItem.sellPrice <= 0)
                return false;

            result = shopItem;
            return true;
        }

        return false;
    }
}
