
using System.Linq;
using Fusion;
using UnityEngine;

public class NetworkResourceWalletTester : NetworkBehaviour
{
    #region REFERENCES
    [Header("Wallet Reference")]
    [SerializeField] private NetworkResourceWallet wallet;

    [Header("Shop Reference")]
    [SerializeField] private NetworkShop networkShop;

    [Header("Auction Reference")]
    [SerializeField] private NetworkTrading networkTrading;
    #endregion

    #region TEST SETTINGS
    [Header("Wallet Test Amounts")]
    [SerializeField, Min(1)] private int testWood = 10;
    [SerializeField, Min(1)] private int testCopperOre = 5;
    [SerializeField, Min(1)] private int testIronOre = 3;

    [Header("Shop Test Settings")]
    [SerializeField, Min(1)] private int testItemID = 1;
    [SerializeField, Min(1)] private int testItemAmount = 1;

    [Header("Auction Test Settings")]
    [SerializeField, Min(1)] private int auctionItemID = 1;
    [SerializeField, Min(1)] private int auctionItemAmount = 1;
    [SerializeField, Min(1)] private int auctionStartingPrice = 10;
    [SerializeField, Min(1)] private int testBidAmount = 20;
    #endregion

    #region DEBUG UI
    [Header("Debug Overlay")]
    [SerializeField] private bool showOverlay = true;

    private string buyResultMessage = "No purchase yet.";
    private string sellResultMessage = "No sale yet.";
    #endregion

    #region UNITY LIFECYCLE

    private void Start()
    {
        SubscribeShopEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeShopEvents();
    }

    private void Update()
    {
        HandleWalletInput();
        HandleShopInput();
        HandleAuctionInput();
    }

    #endregion

    #region EVENT SUBSCRIPTION
    private void SubscribeShopEvents()
    {
        if (networkShop == null)
            return;

        networkShop.OnBuyResult += HandleBuyResult;
        networkShop.OnSellResult += HandleSellResult;
    }

    private void UnsubscribeShopEvents()
    {
        if (networkShop == null)
            return;

        networkShop.OnBuyResult -= HandleBuyResult;
        networkShop.OnSellResult -= HandleSellResult;
    }

    private void HandleBuyResult(bool success)
    {
        buyResultMessage = success ? "BUY SUCCESS" : "BUY FAILED";

        if (success)
            Debug.Log("[Shop Test] BUY SUCCESS!");
        else
            Debug.Log("[Shop Test] BUY FAILED!");
    }

    private void HandleSellResult(bool success)
    {
        sellResultMessage = success ? "SELL SUCCESS" : "SELL FAILED";

        if (success)
            Debug.Log("[Shop Test] SELL SUCCESS!");
        else
            Debug.Log("[Shop Test] SELL FAILED!");
    }
    #endregion

    #region INPUT HANDLING
    private void HandleWalletInput()
    {
        if (!CanRunWalletTest())
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            PrintWalletState();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddResource(
                NetworkResourceWallet.ResourceType.Wood,
                testWood);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AddResource(
                NetworkResourceWallet.ResourceType.CopperOre,
                testCopperOre);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AddResource(
                NetworkResourceWallet.ResourceType.IronOre,
                testIronOre);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            ConvertAllToGold();
    }

    private void HandleShopInput()
    {
        if (!CanRunShopTest())
            return;

        if (Input.GetKeyDown(KeyCode.Alpha6))
            TestBuyItem();

        if (Input.GetKeyDown(KeyCode.Alpha7))
            TestSellItem();
    }

    private void HandleAuctionInput()
    {
        if (!CanRunAuctionTest())
            return;

        if (Input.GetKeyDown(KeyCode.Alpha8))
            PrintAuctionState();

        if (Input.GetKeyDown(KeyCode.Alpha9))
            TestConfigurePlayers();

        if (Input.GetKeyDown(KeyCode.Alpha0))
            TestStartAuction();

        if (Input.GetKeyDown(KeyCode.Minus))
            TestPlaceBid();

        if (Input.GetKeyDown(KeyCode.Equals))
            TestAuctionResult();
    }
    #endregion

    #region AUTHORITY CHECK
    private bool CanRunWalletTest()
    {
        return wallet != null &&
               wallet.Object != null &&
               wallet.Object.IsValid &&
               wallet.Object.HasStateAuthority;
    }

    private bool CanRunShopTest()
    {
        return networkShop != null &&
               networkShop.Object != null &&
               networkShop.Object.IsValid;
    }
    #endregion

    #region WALLET TESTS
    private void PrintWalletState()
    {
        Debug.Log(
            $"[Wallet Test] " +
            $"Wood={wallet.Wood} | " +
            $"CopperOre={wallet.CopperOre} | " +
            $"IronOre={wallet.IronOre} | " +
            $"GoldOre={wallet.GoldOre}");
    }

    private void AddResource(
        NetworkResourceWallet.ResourceType resourceType,
        int amount)
    {
        bool success = wallet.TryAddResource(resourceType, amount);

        Debug.Log(
            $"[Wallet Test] Add {resourceType} x{amount} | " +
            $"Success={success}");

        PrintWalletState();
    }

    private void ConvertAllToGold()
    {
        wallet.ConvertAllToGold();

        Debug.Log("[Wallet Test] Converted all resources to GoldOre.");

        PrintWalletState();
    }
    #endregion

    #region SHOP TESTS
    private void TestBuyItem()
    {
        Debug.Log(
            $"[Shop Test] Request BUY | " +
            $"ItemID={testItemID} | Amount={testItemAmount}");

        networkShop.RequestBuy(testItemID, testItemAmount);
    }

    private void TestSellItem()
    {
        Debug.Log(
            $"[Shop Test] Request SELL | " +
            $"ItemID={testItemID} | Amount={testItemAmount}");

        networkShop.RequestSell(testItemID, testItemAmount);
    }
    #endregion

    #region AUCTION TESTS
    private bool CanRunAuctionTest()
    {
        return networkTrading != null &&
            networkTrading.Object != null &&
            networkTrading.Object.IsValid;
    }

    private void PrintAuctionState()
    {
        Debug.Log(
            $"[Auction Test] " +
            $"State={networkTrading.State} | " +
            $"ItemID={networkTrading.ItemID} | " +
            $"Quantity={networkTrading.Quantity} | " +
            $"StartingPrice={networkTrading.StartingPrice} | " +
            $"CurrentBid={networkTrading.CurrentBid} | " +
            $"HighestBidder={networkTrading.HighestBidder} | " +
            $"Winner={networkTrading.Winner}");
    }

    private void TestConfigurePlayers()
    {
        if (!networkTrading.Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[Auction Test] Only State Authority can configure players.");

            return;
        }

        PlayerRef[] players = new PlayerRef[networkTrading.Runner.ActivePlayers.Count()];

        int index = 0;

        foreach (PlayerRef player in networkTrading.Runner.ActivePlayers)
        {
            if (index >= players.Length)
                break;

            players[index] = player;
            index++;
        }

        if (index < 2)
        {
            Debug.LogWarning(
                "[Auction Test] Need at least 2 players to start auction.");

            return;
        }

        System.Array.Resize(ref players, index);

        bool success = networkTrading.ConfigurePlayers(players);

        Debug.Log(
            $"[Auction Test] Configure Players | " +
            $"Count={players.Length} | Success={success}");
    }

    private void TestStartAuction()
    {
        if (!networkTrading.Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[Auction Test] Only State Authority can start auction.");

            return;
        }

        bool success = networkTrading.TradeItem(
            auctionItemID,
            auctionItemAmount);

        Debug.Log(
            $"[Auction Test] Start Auction | " +
            $"ItemID={auctionItemID} | " +
            $"Amount={auctionItemAmount} | " +
            $"Success={success}");
    }

    private void TestPlaceBid()
    {
        Debug.Log(
            $"[Auction Test] Request Bid | Amount={testBidAmount}");

        networkTrading.RPC_RequestBid(testBidAmount);
    }

    private void TestAuctionResult()
    {
        PrintAuctionState();

        if (networkTrading.State == AuctionState.Completed)
        {
            Debug.Log(
                $"[Auction Test] AUCTION COMPLETED | " +
                $"Winner={networkTrading.Winner} | " +
                $"FinalBid={networkTrading.CurrentBid}");
        }
        else if (networkTrading.State == AuctionState.Active)
        {
            Debug.Log("[Auction Test] Auction is still active.");
        }
        else
        {
            Debug.Log(
                $"[Auction Test] Auction State={networkTrading.State}");
        }
    }
#endregion

    #region DEBUG OVERLAY
    private void OnGUI()
    {
        if (!showOverlay)
            return;

        GUILayout.BeginArea(new Rect(50, 50, 500, 500), GUI.skin.box);

        GUILayout.Label("ĐỪNG TEST TRÊN SCENE NÀY, TEST THẲNG TRÊN SCENE CHÍNH CỦA GAME. CHỈ THAM KHẢO SET UP");

        GUILayout.Space(10);

        GUILayout.Label("Đầu tiên, hãy đảm bảo rằng bạn đã kết nối với mạng và có ít nhất 2 người chơi để thực hiện các bài kiểm tra.");
        GUILayout.Label("Thêm NetworkResourceWallet vào player prefab và NetworkShop vào scene để kiểm tra chức năng mua bán.");
        GUILayout.Label("Thêm NetworkTrading vào scene để kiểm tra chức năng đấu giá.");

        // if (CanRunWalletTest())
        // {
        //     GUILayout.Label("WALLET TEST (STATE AUTHORITY)");
        //     GUILayout.Label("1 - Print wallet state");
        //     GUILayout.Label($"2 - Add Wood x{testWood}");
        //     GUILayout.Label($"3 - Add CopperOre x{testCopperOre}");
        //     GUILayout.Label($"4 - Add IronOre x{testIronOre}");
        //     GUILayout.Label("5 - Convert all resources to Gold");
        // }

        // GUILayout.Space(10);

        // if (CanRunShopTest())
        // {
        //     GUILayout.Label("SHOP TEST");
        //     GUILayout.Label($"6 - Buy ItemID {testItemID} x{testItemAmount}");
        //     GUILayout.Label($"7 - Sell ItemID {testItemID} x{testItemAmount}");

        //     GUILayout.Space(5);
        //     GUILayout.Label($"Buy Result: {buyResultMessage}");
        //     GUILayout.Label($"Sell Result: {sellResultMessage}");
        // }

        // GUILayout.Space(10);

        // if (CanRunAuctionTest())
        // {
        //     GUILayout.Label("AUCTION TEST");

        //     GUILayout.Label("8 - Print auction state");
        //     GUILayout.Label("9 - Configure auction players");
        //     GUILayout.Label("0 - Start auction");
        //     GUILayout.Label("- - Place bid");
        //     GUILayout.Label("= - Print auction result");

        //     GUILayout.Label($"Auction State: {networkTrading.State}");
        //     GUILayout.Label($"Current Bid: {networkTrading.CurrentBid}");
        //     GUILayout.Label($"Highest Bidder: {networkTrading.HighestBidder}");
        //     GUILayout.Label($"Winner: {networkTrading.Winner}");
        // }

        GUILayout.EndArea();
    } 
    #endregion
}
