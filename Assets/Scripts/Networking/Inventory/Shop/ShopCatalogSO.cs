using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopItemOffer
{
    [SerializeField] private ItemSO item;

    [Tooltip("Buying price from Shop")]
    [Min(1), SerializeField] private int buyPrice = 1;

    [Tooltip("Selling price to Shop")]
    [Min(1), SerializeField] private int sellPrice = 1;

    public ItemSO Item => item;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
}

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Online Inventory/ShopCatalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Header("All Catalog Offers")]
    [SerializeField] private List<ShopItemOffer> allOffers = new();

    private List<ShopItemOffer> _consumables;
    private List<ShopItemOffer> _valuables;
    private Dictionary<int, ShopItemOffer> _consumableMap;
    private Dictionary<int, ShopItemOffer> _valuableMap;
    private bool _isInitialized;

    private void OnEnable()
    {
        _isInitialized = false;
    }

    public void Initialize()
    {
        _consumables = new List<ShopItemOffer>();
        _valuables = new List<ShopItemOffer>();
        _consumableMap = new Dictionary<int, ShopItemOffer>();
        _valuableMap = new Dictionary<int, ShopItemOffer>();

        foreach (ShopItemOffer offer in allOffers)
        {
            if (offer?.Item == null) continue;

            // Tự động phân loại dựa theo ItemType của ItemSO
            switch (offer.Item.ItemType)
            {
                case ItemType.Consumable:
                    _consumables.Add(offer);
                    _consumableMap.TryAdd(offer.Item.ItemId, offer);
                    break;

                case ItemType.Valuable:
                    _valuables.Add(offer);
                    _valuableMap.TryAdd(offer.Item.ItemId, offer);
                    break;

                default:
                    // Bỏ qua nếu là ItemType.None
                    break;
            }
        }

        _isInitialized = true;
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _consumables == null)
            Initialize();
    }

    #region ShopSession (Consumables)
    public IReadOnlyList<ShopItemOffer> ConsumableOffers
    {
        get
        {
            EnsureInitialized();
            return _consumables;
        }
    }

    public ShopItemOffer GetConsumableOffer(int itemID)
    {
        EnsureInitialized();
        _consumableMap.TryGetValue(itemID, out var offer);
        return offer;
    }
    #endregion

    #region TradeSession (Valuables)
    public IReadOnlyList<ShopItemOffer> ValuableOffers
    {
        get
        {
            EnsureInitialized();
            return _valuables;
        }
    }

    public ShopItemOffer GetValuableOffer(int itemID)
    {
        EnsureInitialized();
        _valuableMap.TryGetValue(itemID, out var offer);
        return offer;
    }
    #endregion

    public ShopItemOffer GetOffer(int itemID)
    {
        EnsureInitialized();
        if (_consumableMap.TryGetValue(itemID, out var offer)) return offer;
        if (_valuableMap.TryGetValue(itemID, out offer)) return offer;
        return null;
    }
}