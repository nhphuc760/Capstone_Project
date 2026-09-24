using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[Serializable]
public class ShopItemOffer
{
    [SerializeField] private ItemSO item;

    [Tooltip("Set the buy price for this item")] 
    [Min(1), SerializeField] private int buyPrice = 1;

    [Tooltip("Set the sell price for this item")] 
    [Min(1), SerializeField] private int sellPrice = 1;

    public ItemSO Item => item;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
}

public class ShopSession : NetworkBehaviour
{
    [Header("Consumable shop catalog")]
    [SerializeField] private List<ShopItemOffer> offers = new();

    public IEnumerable<ShopItemOffer> ConsumableOffers
    {
        get
        {
            foreach (ShopItemOffer offer in offers)
            {
                if (IsConsumableOffer(offer))
                    yield return offer;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Buy(int itemID, int amount, RpcInfo info = default)
    {
        ProcessBuy(ResolvePlayer(info), itemID, amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Sell(int itemID, int amount, RpcInfo info = default)
    {
        ProcessSell(ResolvePlayer(info), itemID, amount);
    }

    private void ProcessBuy(PlayerRef player, int itemID, int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0)
            return;

        ShopItemOffer offer = GetConsumableOffer(itemID);
        NetworkMoney money = GetMoneyForPlayer(player);
        NetworkInventory inventory = GetInventoryForPlayer(player);

        if (offer == null || money == null || inventory == null)
            return;

        if (!TryGetTotalPrice(offer.BuyPrice, amount, out int totalPrice))
            return;

        if (!money.HasMoney(totalPrice) || !inventory.CanAddItem(itemID, amount))
            return;

        if (!inventory.AddItem(itemID, amount))
            return;

        // Prevent a free item if the balance changed unexpectedly.
        if (!money.RemoveMoney(totalPrice))
            inventory.RemoveItem(itemID, amount);
    }

    private void ProcessSell(PlayerRef player, int itemID, int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0)
            return;

        ShopItemOffer offer = GetConsumableOffer(itemID);
        NetworkMoney money = GetMoneyForPlayer(player);
        NetworkInventory inventory = GetInventoryForPlayer(player);

        if (offer == null || money == null || inventory == null)
            return;

        if (!TryGetTotalPrice(offer.SellPrice, amount, out int totalPrice))
            return;

        if (!inventory.HasItem(itemID, amount))
            return;

        if (!inventory.RemoveItem(itemID, amount))
            return;

        // Restore the item if adding money unexpectedly fails.
        if (!money.AddMoney(totalPrice))
            inventory.AddItem(itemID, amount);
    }

    private ShopItemOffer GetConsumableOffer(int itemID)
    {
        foreach (ShopItemOffer offer in offers)
        {
            if (IsConsumableOffer(offer) && offer.Item.ItemId == itemID)
                return offer;
        }

        return null;
    }

    private static bool IsConsumableOffer(ShopItemOffer offer)
    {
        return offer != null && offer.Item != null && offer.Item.ItemType == ItemType.Consumable;
    }

    private static bool TryGetTotalPrice(int unitPrice, int amount, out int totalPrice)
    {
        long calculatedPrice = (long)unitPrice * amount;
        totalPrice = 0;

        if (unitPrice <= 0 || calculatedPrice > int.MaxValue)
            return false;

        totalPrice = (int)calculatedPrice;
        return true;
    }

    private PlayerRef ResolvePlayer(RpcInfo info)
    {
        return info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
    }

    private static NetworkMoney GetMoneyForPlayer(PlayerRef player)
    {
        foreach (NetworkMoney money in FindObjectsByType<NetworkMoney>(FindObjectsSortMode.None))
        {
            if (money.Object != null && money.Object.InputAuthority == player)
                return money;
        }

        return null;
    }

    private static NetworkInventory GetInventoryForPlayer(PlayerRef player)
    {
        foreach (NetworkInventory inventory in FindObjectsByType<NetworkInventory>(FindObjectsSortMode.None))
        {
            if (inventory.Object != null && inventory.Object.InputAuthority == player)
                return inventory;
        }

        return null;
    }
}
