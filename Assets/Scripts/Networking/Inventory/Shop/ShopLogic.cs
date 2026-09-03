using Fusion;
using UnityEngine;

public class ShopLogic : NetworkBehaviour
{
    public enum ShopResult
    {
        None,
        Success,
        ItemNotFound,
        InvalidAmount,
        NotEnoughMoney,
        InventoryFull,
        Failed
    }

    [Networked]
    public ShopResult Result { get; private set; }

    [Networked]
    public int LastItemID { get; private set; }

    [Networked]
    public int LastAmount { get; private set; }

    [Networked]
    public int LastTotalPrice { get; private set; }

    [Header("Shop")]
    [SerializeField]
    private ItemDatabase database;

    public ItemDatabase Database => database;
    public override void Spawned()
    {
        base.Spawned();

        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("ItemData");

            if (database == null)
            {
                Debug.LogError(
                    $"{name}: ItemDatabase not found."
                );
            }
        }
    }

    public bool Buy(PlayerRef player, int itemID, int amount, int price)
    {
        if (!Object.HasStateAuthority)
            return false;

        Result = ShopResult.None;

        if (amount <= 0)
        {
            Result = ShopResult.InvalidAmount;
            return false;
        }

        if (price <= 0)
        {
            Result = ShopResult.Failed;
            return false;
        }

        // =========================
        // ITEM
        // =========================

        ItemSO item = database.GetItem(itemID);

        if (item == null)
        {
            Result = ShopResult.ItemNotFound;
            return false;
        }

        // =========================
        // PLAYER
        // =========================

        NetworkInventory inventory =
            GetInventory(player);

        NetworkMoney money =
            GetMoney(player);

        if (inventory == null ||
            money == null)
        {
            Result = ShopResult.Failed;
            return false;
        }

        // =========================
        // PRICE
        // =========================

        int totalPrice = price * amount;

        if (!money.HasMoney(totalPrice))
        {
            Result = ShopResult.NotEnoughMoney;
            return false;
        }

        // =========================
        // INVENTORY
        // =========================

        if (!inventory.AddItem(itemID, amount))
        {
            Result = ShopResult.InventoryFull;
            return false;
        }

        // =========================
        // TRANSACTION
        // =========================

        if (!money.RemoveMoney(totalPrice))
        {
            Result = ShopResult.Failed;
            return false;
        }

        if (!inventory.AddItem(itemID, amount))
        {
            // Rollback money
            money.AddMoney(totalPrice);

            Result = ShopResult.Failed;
            return false;
        }

        // =========================
        // SUCCESS
        // =========================

        LastItemID = itemID;
        LastAmount = amount;
        LastTotalPrice = totalPrice;

        Result = ShopResult.Success;

        Debug.Log(
            $"SHOP BUY | " +
            $"Player={player} | " +
            $"Item={itemID} x{amount} | " +
            $"Price={totalPrice}"
        );

        return true;
    }

    private NetworkInventory GetInventory(PlayerRef player)
    {
        // if (!player.IsValid)
        //     return null;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        {
            return null;
        }

        return playerObject.GetComponent<NetworkInventory>();
    }

    private NetworkMoney GetMoney(PlayerRef player)
    {
        // if (!player.IsValid)
        //     return null;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        {
            return null;
        }

        return playerObject.GetComponent<NetworkMoney>();
    }
}