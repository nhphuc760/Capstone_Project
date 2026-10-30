
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ValuableShopSO", menuName = "Shop/Valuable Shop")]
public class ValuableShopSO : ScriptableObject
{
    [Serializable]
    public class ValuableShopItem
    {
        [Header("Product")]
        public ItemSO itemSO;

        [Header("Valuable Price")]
        [Min(1)] public int valuablePrice = 500;
    }

    [Header("Valuable Products")]
    [SerializeField] private List<ValuableShopItem> shopItems = new();

    public IReadOnlyList<ValuableShopItem> ShopItems => shopItems;

    public bool TryGetItem(int itemID, out ValuableShopItem result)
    {
        result = null;

        foreach (ValuableShopItem shopItem in shopItems)
        {
            if (shopItem == null || shopItem.itemSO == null)
                continue;

            if (shopItem.itemSO.ItemId != itemID)
                continue;

            if (shopItem.itemSO.itemType != ItemType.Valuable)
                return false;

            if (shopItem.valuablePrice <= 0)
                return false;

            result = shopItem;
            return true;
        }

        return false;
    }

    public bool TryGetValuablePrice(int itemID, out int price)
    {
        price = 0;

        if (!TryGetItem(itemID, out ValuableShopItem shopItem))
            return false;

        price = shopItem.valuablePrice;
        return true;
    }
}
