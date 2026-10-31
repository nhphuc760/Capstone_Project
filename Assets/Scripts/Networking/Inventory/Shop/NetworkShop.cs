
using System.Linq;
using Fusion;
using UnityEngine;

public class NetworkShop : NetworkBehaviour
{
    [Header("Shop Data")]
    [SerializeField] private ConsumableShopSO consumableShopSO;

    [Header("Settings")]
    [SerializeField, Min(1)]
    private int maxTransactionAmount = 99;

    public void RequestBuy(int itemID, int amount)
    {
        if (Object == null || !Object.IsValid)
            return;

        RPC_RequestBuy(itemID, amount);
    }

    public void RequestSell(int itemID, int amount)
    {
        if (Object == null || !Object.IsValid)
            return;

        RPC_RequestSell(itemID, amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestBuy(
        int itemID,
        int amount,
        RpcInfo info = default)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        PlayerRef player = info.Source;

        if (!ValidateRequest(player, itemID, amount))
            return;

        if (!TryGetShopItem(itemID, out ConsumableShopSO.ConsumableShopItem shopItem))
            return;

        if (!IsConsumable(shopItem.itemSO))
            return;

        if (!TryGetPlayerComponents(
                player,
                out NetworkResourceWallet wallet,
                out NetworkInventory inventory))
            return;

        if (!TryGetTotalPrice(
                shopItem.buyPriceGold,
                amount,
                out int totalPrice))
            return;

        if (!inventory.CanAddItem(itemID, amount))
            return;

        if (!wallet.CanAffordGold(totalPrice))
            return;

        if (!wallet.TrySpendGold(totalPrice))
            return;

        if (!inventory.TryAddItem(itemID, amount))
        {
            bool refunded = wallet.TryAddGold(totalPrice);

            Debug.LogError(
                $"[NetworkShop] AddItem failed. " +
                $"Refunded={refunded}, Player={player}");

            return;
        }

        Debug.Log(
            $"[NetworkShop] BUY SUCCESS | " +
            $"Player={player} | ItemID={itemID} | " +
            $"Amount={amount} | Price={totalPrice} Gold");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSell(
        int itemID,
        int amount,
        RpcInfo info = default)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        PlayerRef player = info.Source;

        if (!ValidateRequest(player, itemID, amount))
            return;

        if (!TryGetShopItem(itemID, out ConsumableShopSO.ConsumableShopItem shopItem))
            return;

        if (!IsConsumable(shopItem.itemSO))
            return;

        if (!TryGetPlayerComponents(
                player,
                out NetworkResourceWallet wallet,
                out NetworkInventory inventory))
            return;

        if (!TryGetTotalPrice(
                shopItem.sellPriceGold,
                amount,
                out int totalPrice))
            return;

        if (!inventory.HasItem(itemID, amount))
            return;

        if (!inventory.RemoveItem(itemID, amount))
            return;

        if (!wallet.TryAddGold(totalPrice))
        {
            bool itemRestored = inventory.TryAddItem(itemID, amount);

            Debug.LogError(
                $"[NetworkShop] Payment failed. " +
                $"ItemRestored={itemRestored}, Player={player}");

            return;
        }

        Debug.Log(
            $"[NetworkShop] SELL SUCCESS | " +
            $"Player={player} | ItemID={itemID} | " +
            $"Amount={amount} | ReceivedGold={totalPrice} Gold");
    }

    private bool ValidateRequest(
        PlayerRef player,
        int itemID,
        int amount)
    {
        if (player == PlayerRef.None)
            return false;

        if (Runner == null || !Runner.ActivePlayers.Contains(player))
            return false;

        if (itemID <= 0)
            return false;

        if (amount <= 0 || amount > maxTransactionAmount)
            return false;

        return true;
    }

    private bool TryGetShopItem(
        int itemID,
        out ConsumableShopSO.ConsumableShopItem shopItem)
    {
        shopItem = null;

        if (consumableShopSO == null)
        {
            Debug.LogError(
                "[NetworkShop] ConsumableShopSO is not assigned.");

            return false;
        }

        return consumableShopSO.TryGetItem(itemID, out shopItem);
    }

    private bool IsConsumable(ItemSO item)
    {
        return item != null &&
               item.ItemType == ItemType.Consumable;
    }

    private bool TryGetPlayerComponents(
        PlayerRef player,
        out NetworkResourceWallet wallet,
        out NetworkInventory inventory)
    {
        wallet = null;
        inventory = null;

        if (!Runner.TryGetPlayerObject(
                player,
                out NetworkObject playerObject))
        {
            Debug.LogError(
                $"[NetworkShop] PlayerObject not found: {player}");

            return false;
        }

        wallet = playerObject.GetComponent<NetworkResourceWallet>();
        inventory = playerObject.GetComponent<NetworkInventory>();

        if (wallet == null || inventory == null)
        {
            Debug.LogError(
                $"[NetworkShop] Missing Wallet or Inventory on {player}");

            return false;
        }

        return true;
    }

    private bool TryGetTotalPrice(
        int unitPrice,
        int amount,
        out int totalPrice)
    {
        totalPrice = 0;

        if (unitPrice <= 0 || amount <= 0)
            return false;

        long calculatedPrice = (long)unitPrice * amount;

        if (calculatedPrice > int.MaxValue)
            return false;

        totalPrice = (int)calculatedPrice;
        return true;
    }
}
