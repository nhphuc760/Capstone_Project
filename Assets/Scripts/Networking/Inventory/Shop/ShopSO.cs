
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

        [Header("Consumable Prices")]
        [Min(1)] public int buyPrice;
        [Min(1)] public int sellPrice;

        [Header("Valuable Price")]
        [Min(1)] public int valuablePrice;
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

            if (shopItem.itemSO.itemType == ItemType.Consumable)
            {
                if (shopItem.buyPrice <= 0 || shopItem.sellPrice <= 0)
                    return false;
            }
            else if (shopItem.itemSO.itemType == ItemType.Valuable)
            {
                if (shopItem.valuablePrice <= 0)
                    return false;
            }
            else
            {
                return false;
            }

            result = shopItem;
            return true;
        }

        return false;
    }

    public bool TryGetValuablePrice(int itemID, out int price)
    {
        price = 0;

        if (!TryGetItem(itemID, out ShopItem shopItem))
            return false;

        if (shopItem.itemSO.itemType != ItemType.Valuable)
            return false;

        price = shopItem.valuablePrice;
        return true;
    }
}
