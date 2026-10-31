
using Fusion;
using UnityEngine;

public class NetworkResourceWalletTester : NetworkBehaviour
{
    [Header("Wallet Reference")]
    [SerializeField] private NetworkResourceWallet wallet;

    [Header("Shop Reference")]
    [SerializeField] private NetworkShop networkShop;

    [Header("Test Amounts")]
    [SerializeField, Min(1)] private int testWood = 10;
    [SerializeField, Min(1)] private int testCopperOre = 5;
    [SerializeField, Min(1)] private int testIronOre = 3;

    [Header("Shop Test Settings")]
    [SerializeField, Min(1)] private int testItemID = 1;
    [SerializeField, Min(1)] private int testItemAmount = 1;

    [Header("Debug Overlay")]
    [SerializeField] private bool showOverlay = true;

    private void Update()
    {
        // Wallet tests: Only State Authority can modify resources.
        if (CanRunWalletTest())
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                PrintWalletState();

            if (Input.GetKeyDown(KeyCode.Alpha2))
                AddResource(NetworkResourceWallet.ResourceType.Wood, testWood);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                AddResource(NetworkResourceWallet.ResourceType.CopperOre, testCopperOre);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                AddResource(NetworkResourceWallet.ResourceType.IronOre, testIronOre);

            if (Input.GetKeyDown(KeyCode.Alpha5))
                ConvertAllToGold();
        }

        // Shop tests: Can be requested by any player.
        if (CanRunShopTest())
        {
            if (Input.GetKeyDown(KeyCode.Alpha6))
                TestBuyItem();

            if (Input.GetKeyDown(KeyCode.Alpha7))
                TestSellItem();
        }
    }

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

    #region Wallet Tests

    private void PrintWalletState()
    {
        Debug.Log(
            $"[Wallet Test] Current Resources\n" +
            $"Wood: {wallet.Wood}\n" +
            $"Copper Ore: {wallet.CopperOre}\n" +
            $"Iron Ore: {wallet.IronOre}\n" +
            $"Gold Ore: {wallet.GoldOre}\n" +
            $"Total Gold Value: {wallet.GetTotalGoldValue()}"
        );
    }

    private void AddResource(
        NetworkResourceWallet.ResourceType type,
        int amount)
    {
        bool success = wallet.TryAddResource(type, amount);

        Debug.Log(
            $"[Wallet Test] Add Resource: {type} | " +
            $"Amount: {amount} | " +
            $"Result: {(success ? "SUCCESS" : "FAILED")}"
        );

        PrintWalletState();
    }

    private void ConvertAllToGold()
    {
        Debug.Log("[Wallet Test] Trying to convert all resources to Gold...");

        long previousGold = wallet.GoldOre;

        bool success = wallet.ConvertAllToGold();

        long receivedGold = wallet.GoldOre - previousGold;

        Debug.Log(
            $"[Wallet Test] Conversion Result: {(success ? "SUCCESS" : "FAILED")}\n" +
            $"Gold Received: {receivedGold}"
        );

        PrintWalletState();
    }

    #endregion

    #region Shop Tests

    private void TestBuyItem()
    {
        Debug.Log(
            $"[Shop Test] Request BUY | " +
            $"ItemID={testItemID} | Amount={testItemAmount}"
        );

        networkShop.RequestBuy(testItemID, testItemAmount);
    }

    private void TestSellItem()
    {
        Debug.Log(
            $"[Shop Test] Request SELL | " +
            $"ItemID={testItemID} | Amount={testItemAmount}"
        );

        networkShop.RequestSell(testItemID, testItemAmount);
    }

    #endregion

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        bool canTestWallet = CanRunWalletTest();
        bool canTestShop = CanRunShopTest();

        if (!canTestWallet && !canTestShop)
            return;

        GUILayout.BeginArea(
            new Rect(10, 10, 340, 520),
            GUI.skin.box
        );

        GUILayout.Label("Network Resource Wallet Tester");

        GUILayout.Space(10);

        #region Wallet UI

        if (canTestWallet)
        {
            GUILayout.Label("--- WALLET TEST ---");

            if (GUILayout.Button("Print Wallet State (1)"))
                PrintWalletState();

            if (GUILayout.Button($"Add Wood +{testWood} (2)"))
                AddResource(NetworkResourceWallet.ResourceType.Wood, testWood);

            if (GUILayout.Button($"Add Copper Ore +{testCopperOre} (3)"))
                AddResource(NetworkResourceWallet.ResourceType.CopperOre, testCopperOre);

            if (GUILayout.Button($"Add Iron Ore +{testIronOre} (4)"))
                AddResource(NetworkResourceWallet.ResourceType.IronOre, testIronOre);

            GUI.backgroundColor = Color.yellow;

            if (GUILayout.Button("Convert All Resources To Gold (5)", GUILayout.Height(30)))
                ConvertAllToGold();

            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            GUILayout.Label($"Wood: {wallet.Wood}");
            GUILayout.Label($"Copper Ore: {wallet.CopperOre}");
            GUILayout.Label($"Iron Ore: {wallet.IronOre}");
            GUILayout.Label($"Gold Ore: {wallet.GoldOre}");
            GUILayout.Label($"Total Gold Value: {wallet.GetTotalGoldValue()}");
        }

        #endregion

        #region Shop UI

        if (canTestShop)
        {
            GUILayout.Space(15);
            GUILayout.Label("--- SHOP TEST ---");

            GUILayout.Label($"Item ID: {testItemID}");
            GUILayout.Label($"Amount: {testItemAmount}");

            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("BUY ITEM (6)", GUILayout.Height(35)))
                TestBuyItem();

            GUI.backgroundColor = Color.red;

            if (GUILayout.Button("SELL ITEM (7)", GUILayout.Height(35)))
                TestSellItem();

            GUI.backgroundColor = Color.white;
        }

        #endregion

        GUILayout.EndArea();
    }
}
