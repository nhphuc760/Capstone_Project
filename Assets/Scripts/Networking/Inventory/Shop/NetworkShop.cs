
using System;
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

    public event Action<bool> OnBuyResult;
    public event Action<bool> OnSellResult;

    #region PUBLIC REQUEST
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
    #endregion

    #region BUY ITEM  
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestBuy(
        int itemID,
        int amount,
        RpcInfo info = default)
    {
        Debug.Log(
            $"[NetworkShop] BUY RPC RECEIVED | " +
            $"Source={info.Source} | ItemID={itemID} | Amount={amount}");

        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogError("[NetworkShop] BUY FAILED: No State Authority.");
            return;
        }

        PlayerRef player = info.Source;

        if (!ValidateRequest(player, itemID, amount))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: Invalid request.");
            SendBuyResult(player, false);
            return;
        }

        if (!TryGetShopItem(
            itemID,
            out ConsumableShopSO.ConsumableShopItem shopItem))
        {
            Debug.LogError($"[NetworkShop] BUY FAILED: ItemID {itemID} not found in ShopSO.");
            SendBuyResult(player, false);
            return;
        }

        if (!IsConsumable(shopItem.itemSO))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: Item is not Consumable.");
            SendBuyResult(player, false);
            return;
        }

        if (!TryGetPlayerComponents(
            player,
            out NetworkResourceWallet wallet,
            out NetworkInventory inventory))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: Cannot get player components.");
            SendBuyResult(player, false);
            return;
        }

        if (!TryGetTotalPrice(
            shopItem.buyPriceGold,
            amount,
            out int totalPrice))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: Invalid total price.");
            SendBuyResult(player, false);
            return;
        }

        Debug.Log(
            $"[NetworkShop] BUY CHECK | " +
            $"Player={player} | UnitPrice={shopItem.buyPriceGold} | " +
            $"Amount={amount} | TotalPrice={totalPrice} | " +
            $"GoldBefore={wallet.GoldOre}");

        if (!inventory.CanAddItem(itemID, amount))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: Inventory is full or cannot add item.");
            SendBuyResult(player, false);
            return;
        }

        if (!wallet.CanAffordGold(totalPrice))
        {
            Debug.LogError(
                $"[NetworkShop] BUY FAILED: Not enough Gold. " +
                $"Required={totalPrice} | Available={wallet.GoldOre}");

            SendBuyResult(player, false);
            return;
        }

        if (!wallet.TrySpendGold(totalPrice))
        {
            Debug.LogError("[NetworkShop] BUY FAILED: TrySpendGold returned false.");
            SendBuyResult(player, false);
            return;
        }

        Debug.Log(
            $"[NetworkShop] GOLD DEDUCTED | " +
            $"GoldAfter={wallet.GoldOre}");

        if (!inventory.TryAddItem(itemID, amount))
        {
            bool refunded = wallet.TryAddGold(totalPrice);

            Debug.LogError(
                $"[NetworkShop] BUY FAILED: TryAddItem returned false. " +
                $"Refunded={refunded}");

            SendBuyResult(player, false);
            return;
        }

        Debug.Log(
            $"[NetworkShop] BUY SUCCESS | " +
            $"Player={player} | ItemID={itemID} | Amount={amount} | " +
            $"Price={totalPrice} | GoldAfter={wallet.GoldOre}");

        SendBuyResult(player, true);

        Debug.Log(
        $"[NetworkShop] AUTHORITY CHECK | " +
        $"LocalPlayer={Runner.LocalPlayer} | " +
        $"RequestPlayer={player} | " +
        $"ShopStateAuthority={Object.StateAuthority} | " +
        $"WalletStateAuthority={wallet.Object.StateAuthority} | " +
        $"WalletHasStateAuthority={wallet.Object.HasStateAuthority}");
    }
    #endregion

    #region SELL ITEM
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSell(
        int itemID,
        int amount,
        RpcInfo info = default)
    {
        Debug.Log(
            $"[NetworkShop] SELL RPC RECEIVED | " +
            $"Source={info.Source} | ItemID={itemID} | Amount={amount}");

        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogError("[NetworkShop] SELL FAILED: No State Authority.");
            return;
        }

        PlayerRef player = info.Source;

        if (!ValidateRequest(player, itemID, amount))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: Invalid request.");
            SendSellResult(player, false);
            return;
        }

        if (!TryGetShopItem(
            itemID,
            out ConsumableShopSO.ConsumableShopItem shopItem))
        {
            Debug.LogError($"[NetworkShop] SELL FAILED: ItemID {itemID} not found in ShopSO.");
            SendSellResult(player, false);
            return;
        }

        if (!IsConsumable(shopItem.itemSO))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: Item is not Consumable.");
            SendSellResult(player, false);
            return;
        }

        if (!TryGetPlayerComponents(
            player,
            out NetworkResourceWallet wallet,
            out NetworkInventory inventory))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: Cannot get player components.");
            SendSellResult(player, false);
            return;
        }

        if (!TryGetTotalPrice(
            shopItem.sellPriceGold,
            amount,
            out int totalPrice))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: Invalid total price.");
            SendSellResult(player, false);
            return;
        }

        Debug.Log(
            $"[NetworkShop] SELL CHECK | " +
            $"Player={player} | UnitPrice={shopItem.sellPriceGold} | " +
            $"Amount={amount} | TotalPrice={totalPrice} | " +
            $"GoldBefore={wallet.GoldOre}");

        if (!inventory.HasItem(itemID, amount))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: Not enough items in inventory.");
            SendSellResult(player, false);
            return;
        }

        if (!inventory.RemoveItem(itemID, amount))
        {
            Debug.LogError("[NetworkShop] SELL FAILED: RemoveItem returned false.");
            SendSellResult(player, false);
            return;
        }

        if (!wallet.TryAddGold(totalPrice))
        {
            bool itemRestored = inventory.TryAddItem(itemID, amount);

            Debug.LogError(
                $"[NetworkShop] SELL FAILED: TryAddGold returned false. " +
                $"ItemRestored={itemRestored}");

            SendSellResult(player, false);
            return;
        }

        Debug.Log(
            $"[NetworkShop] SELL SUCCESS | " +
            $"Player={player} | ItemID={itemID} | Amount={amount} | " +
            $"ReceivedGold={totalPrice} | GoldAfter={wallet.GoldOre}");

        SendSellResult(player, true);
    }
    #endregion

    #region BUY RESULT
    private void SendBuyResult(PlayerRef player, bool success)
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        RPC_BuyResult(player, success);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BuyResult(
        PlayerRef targetPlayer,
        bool success,
        RpcInfo info = default)
    {
        // Only the requesting player receives the result.
        if (Runner.LocalPlayer != targetPlayer)
            return;

        OnBuyResult?.Invoke(success);
    }
    #endregion

    #region SELL RESULT
    private void SendSellResult(PlayerRef player, bool success)
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        RPC_SellResult(player, success);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SellResult(
        PlayerRef targetPlayer,
        bool success,
        RpcInfo info = default)
    {
        // Only the requesting player receives the result.
        if (Runner.LocalPlayer != targetPlayer)
            return;

        OnSellResult?.Invoke(success);
    }
    #endregion

    #region VALIDATION
    private bool ValidateRequest(PlayerRef player, int itemID, int amount)
    {
        if (player == PlayerRef.None)
        {
            Debug.LogError(
                "[NetworkShop] Invalid request: PlayerRef is None.");

            return false;
        }

        if (Runner == null)
        {
            Debug.LogError(
                "[NetworkShop] Invalid request: Runner is null.");

            return false;
        }

        if (!Runner.ActivePlayers.Contains(player))
        {
            Debug.LogError(
                $"[NetworkShop] Invalid request: Player {player} is not active.");

            return false;
        }

        if (itemID <= 0)
        {
            Debug.LogError(
                $"[NetworkShop] Invalid request: Invalid ItemID={itemID}.");

            return false;
        }

        if (amount <= 0)
        {
            Debug.LogError(
                $"[NetworkShop] Invalid request: Amount must be greater than 0. Amount={amount}");

            return false;
        }

        if (amount > maxTransactionAmount)
        {
            Debug.LogError(
                $"[NetworkShop] Invalid request: Amount exceeds limit. " +
                $"Amount={amount} | Max={maxTransactionAmount}");

            return false;
        }

        Debug.Log(
            $"[NetworkShop] Request validation SUCCESS | " +
            $"Player={player} | ItemID={itemID} | Amount={amount}");

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
    #endregion
}
