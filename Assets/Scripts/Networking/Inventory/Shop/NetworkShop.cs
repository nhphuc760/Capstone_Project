using System.Linq;
using Fusion;
using UnityEngine;

public class NetworkShop : NetworkBehaviour
{
    [Header("Shop Data")]
    [SerializeField] private ShopSO shopSO;

    [Header("Settings")]
    [SerializeField, Min(1)]
    private int maxTransactionAmount = 99;

    public void RequestBuy(int itemID, int amount)
    {
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[NetworkShop] Shop NetworkObject is invalid.");
            return;
        }

        RPC_RequestBuy(itemID, amount);
    }

    public void RequestSell(int itemID, int amount)
    {
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[ConsumableShop] Shop NetworkObject is invalid.");
            return;
        }

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

        if (!TryGetShopItem(itemID, out ShopSO.ShopItem shopItem))
            return;

        if (!IsConsumable(shopItem.itemSO))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Item {itemID} is not a Consumable.");
            return;
        }

        if (!TryGetPlayerComponents(
                player,
                out NetworkResourceWallet wallet,
                out NetworkInventory inventory))
            return;

        if (!TryGetTotalPrice(
                shopItem.buyPrice,
                amount,
                out int totalPrice))
        {
            Debug.LogWarning("[ConsumableShop] Invalid total buy price.");
            return;
        }

        if (!inventory.CanAddItem(itemID, amount))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Player {player} has no inventory space.");
            return;
        }

        if (!wallet.CanAfford(totalPrice))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Player {player} cannot afford {totalPrice}.");
            return;
        }

        // Thanh toán bằng tài nguyên trước khi thêm vật phẩm.
        if (!wallet.TrySpendValue(totalPrice))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Payment failed for player {player}.");
            return;
        }

        // Thêm vật phẩm vào Inventory.
        if (!inventory.CanAddItem(itemID, amount))
        {
            // Hoàn lại giá trị đã thanh toán nếu thêm vật phẩm thất bại.
            bool refunded = wallet.TryAddValueAsWood(totalPrice);

            Debug.LogError(
                $"[ConsumableShop] AddItem failed. " +
                $"Refunded={refunded}, Player={player}");

            return;
        }

        Debug.Log(
            $"[ConsumableShop] BUY SUCCESS | " +
            $"Player={player} | ItemID={itemID} | " +
            $"Amount={amount} | Price={totalPrice}");
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

        if (!TryGetShopItem(itemID, out ShopSO.ShopItem shopItem))
            return;

        if (!IsConsumable(shopItem.itemSO))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Item {itemID} is not a Consumable.");
            return;
        }

        if (!TryGetPlayerComponents(
                player,
                out NetworkResourceWallet wallet,
                out NetworkInventory inventory))
            return;

        if (!TryGetTotalPrice(
                shopItem.sellPrice,
                amount,
                out int totalPrice))
        {
            Debug.LogWarning("[ConsumableShop] Invalid total sell price.");
            return;
        }

        if (!inventory.HasItem(itemID, amount))
        {
            Debug.LogWarning(
                $"[ConsumableShop] Player {player} does not have enough items.");
            return;
        }

        // Trừ vật phẩm trước khi trả tiền.
        if (!inventory.RemoveItem(itemID, amount))
        {
            Debug.LogWarning(
                $"[ConsumableShop] RemoveItem failed for player {player}.");
            return;
        }

        // Giá bán được trả dưới dạng Wood.
        if (!wallet.TryAddValueAsWood(totalPrice))
        {
            // Hoàn lại vật phẩm nếu không thể trả tiền.
            bool itemRestored = inventory.CanAddItem(itemID, amount);

            Debug.LogError(
                $"[ConsumableShop] Payment failed. " +
                $"ItemRestored={itemRestored}, Player={player}");

            return;
        }

        Debug.Log(
            $"[ConsumableShop] SELL SUCCESS | " +
            $"Player={player} | ItemID={itemID} | " +
            $"Amount={amount} | ReceivedValue={totalPrice} Wood");
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
        out ShopSO.ShopItem shopItem)
    {
        shopItem = null;

        if (shopSO == null)
        {
            Debug.LogWarning("[ConsumableShop] ShopSO is not assigned.");
            return false;
        }

        if (!shopSO.TryGetItem(itemID, out shopItem))
        {
            Debug.LogWarning(
                $"[ConsumableShop] ItemID {itemID} was not found in ShopSO.");
            return false;
        }

        return true;
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
            Debug.LogWarning(
                $"[ConsumableShop] PlayerObject not found: {player}");
            return false;
        }

        wallet = playerObject.GetComponent<NetworkResourceWallet>();
        inventory = playerObject.GetComponent<NetworkInventory>();

        if (wallet == null)
        {
            Debug.LogWarning(
                $"[ConsumableShop] NetworkResourceWallet missing on {player}.");
            return false;
        }

        if (inventory == null)
        {
            Debug.LogWarning(
                $"[ConsumableShop] NetworkInventory missing on {player}.");
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