
using Fusion;
using UnityEngine;

public class NetworkResourceWalletTester : NetworkBehaviour
{
    [Header("Wallet Reference")]
    [SerializeField] private NetworkResourceWallet wallet;

    [Header("Test Amounts")]
    [SerializeField, Min(1)] private int testWood = 10;
    [SerializeField, Min(1)] private int testCopperOre = 5;
    [SerializeField, Min(1)] private int testIronOre = 3;
    [SerializeField, Min(1)] private int testGoldOre = 2;

    [Header("Test Values")]
    [SerializeField, Min(1)] private int testSpendPrice = 20;
    [SerializeField, Min(1)] private int testSellValue = 10;

    private void Update()
    {
        if (!CanRunTest())
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            PrintWalletState();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddResource(NetworkResourceWallet.ResourceType.Wood, testWood);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AddResource(NetworkResourceWallet.ResourceType.CopperOre, testCopperOre);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AddResource(NetworkResourceWallet.ResourceType.IronOre, testIronOre);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            AddResource(NetworkResourceWallet.ResourceType.GoldOre, testGoldOre);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SpendValue();

        if (Input.GetKeyDown(KeyCode.Alpha7))
            AddValueAsWood();

        if (Input.GetKeyDown(KeyCode.Alpha8))
            CheckAffordability();
    }

    private bool CanRunTest()
    {
        if (wallet == null)
            return false;

        if (wallet.Object == null || !wallet.Object.IsValid)
            return false;

        // Chỉ State Authority được thực hiện test.
        return wallet.Object.HasStateAuthority;
    }

    private void PrintWalletState()
    {
        Debug.Log(
            $"[Wallet Test] Current Resources\n" +
            $"Wood: {wallet.Wood}\n" +
            $"CopperOre: {wallet.CopperOre}\n" +
            $"IronOre: {wallet.IronOre}\n" +
            $"GoldOre: {wallet.GoldOre}\n" +
            $"Total Value: {wallet.GetTotalValue()}"
        );
    }

    private void AddResource(
        NetworkResourceWallet.ResourceType type,
        int amount)
    {
        bool success = wallet.TryAddResource(type, amount);

        Debug.Log(
            $"[Wallet Test] Add Resource: {type} | Amount: {amount} | " +
            $"Result: {(success ? "SUCCESS" : "FAILED")}"
        );

        PrintWalletState();
    }

    private void SpendValue()
    {
        Debug.Log(
            $"[Wallet Test] Trying to spend: {testSpendPrice}"
        );

        bool success = wallet.TrySpendValue(testSpendPrice);

        Debug.Log(
            $"[Wallet Test] Spend Result: {(success ? "SUCCESS" : "FAILED")}"
        );

        PrintWalletState();
    }

    private void AddValueAsWood()
    {
        Debug.Log(
            $"[Wallet Test] Trying to add value: {testSellValue}"
        );

        bool success = wallet.TryAddValueAsWood(testSellValue);

        Debug.Log(
            $"[Wallet Test] Add Value Result: {(success ? "SUCCESS" : "FAILED")}"
        );

        PrintWalletState();
    }

    private void CheckAffordability()
    {
        bool canAfford = wallet.CanAfford(testSpendPrice);

        Debug.Log(
            $"[Wallet Test] Can Afford {testSpendPrice}: {canAfford}\n" +
            $"Current Total Value: {wallet.GetTotalValue()}"
        );
    }

    private void OnGui()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("NetworkResourceWallet Tester");
        GUILayout.Space(10);

        if (GUILayout.Button("Print Wallet State (Key: 1)"))
            PrintWalletState();

        if (GUILayout.Button($"Add Wood ({testWood}) (Key: 2)"))
            AddResource(NetworkResourceWallet.ResourceType.Wood, testWood);

        if (GUILayout.Button($"Add Copper Ore ({testCopperOre}) (Key: 3)"))
            AddResource(NetworkResourceWallet.ResourceType.CopperOre, testCopperOre);

        if (GUILayout.Button($"Add Iron Ore ({testIronOre}) (Key: 4)"))
            AddResource(NetworkResourceWallet.ResourceType.IronOre, testIronOre);

        if (GUILayout.Button($"Add Gold Ore ({testGoldOre}) (Key: 5)"))
            AddResource(NetworkResourceWallet.ResourceType.GoldOre, testGoldOre);

        if (GUILayout.Button($"Spend Value ({testSpendPrice}) (Key: 6)"))
            SpendValue();

        if (GUILayout.Button($"Add Value as Wood ({testSellValue}) (Key: 7)"))
            AddValueAsWood();

        if (GUILayout.Button($"Check Affordability ({testSpendPrice}) (Key: 8)"))
            CheckAffordability();

        GUILayout.EndArea();
    }
}
