
using Fusion;
using UnityEngine;

public class NetworkResourceWallet : NetworkBehaviour
{
    public enum ResourceType
    {
        Wood,
        CopperOre,
        IronOre,
        GoldOre
    }

    [Header("Network Resources")]
    [Networked] public int Wood { get; private set; }
    [Networked] public int CopperOre { get; private set; }
    [Networked] public int IronOre { get; private set; }
    [Networked] public int GoldOre { get; private set; }

    [Header("Starting Resources")]
    [SerializeField, Min(0)] private int startingWood;
    [SerializeField, Min(0)] private int startingCopperOre;
    [SerializeField, Min(0)] private int startingIronOre;
    [SerializeField, Min(0)] private int startingGoldOre;

    [Header("Gold Conversion Rates")]
    [SerializeField, Min(1)] private int woodPerGold = 5;
    [SerializeField, Min(1)] private int copperPerGold = 4;
    [SerializeField, Min(1)] private int ironPerGold = 3;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        Wood = Mathf.Max(0, startingWood);
        CopperOre = Mathf.Max(0, startingCopperOre);
        IronOre = Mathf.Max(0, startingIronOre);
        GoldOre = Mathf.Max(0, startingGoldOre);
    }

    // Tính tổng Gold có thể nhận được từ tài nguyên hiện tại.
    public long GetTotalGoldValue()
    {
        if (woodPerGold <= 0 || copperPerGold <= 0 || ironPerGold <= 0)
            return GoldOre;

        return (long)GoldOre
             + Wood / woodPerGold
             + CopperOre / copperPerGold
             + IronOre / ironPerGold;
    }

    // Kiểm tra người chơi có đủ Gold để mua hàng hay không.
    public bool CanAffordGold(int price)
    {
        return price > 0 && GetTotalGoldValue() >= price;
    }

    // Quy đổi toàn bộ tài nguyên có thể quy đổi thành Gold.
    public bool ConvertAllToGold()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (woodPerGold <= 0 || copperPerGold <= 0 || ironPerGold <= 0)
            return false;

        int goldFromWood = Wood / woodPerGold;
        int goldFromCopper = CopperOre / copperPerGold;
        int goldFromIron = IronOre / ironPerGold;

        long totalGold = (long)goldFromWood
                       + goldFromCopper
                       + goldFromIron;

        if (totalGold > int.MaxValue - (long)GoldOre)
            return false;

        Wood -= goldFromWood * woodPerGold;
        CopperOre -= goldFromCopper * copperPerGold;
        IronOre -= goldFromIron * ironPerGold;

        GoldOre += (int)totalGold;

        Debug.Log(
            $"[Wallet] Convert To Gold SUCCESS\n" +
            $"Wood: -{goldFromWood * woodPerGold} | Gold: +{goldFromWood}\n" +
            $"Copper: -{goldFromCopper * copperPerGold} | Gold: +{goldFromCopper}\n" +
            $"Iron: -{goldFromIron * ironPerGold} | Gold: +{goldFromIron}\n" +
            $"Total Gold Received: {totalGold}"
        );

        return true;
    }

    // Thanh toán bằng Gold.
    // Tự động quy đổi tài nguyên trước khi trừ Gold.
    public bool TrySpendGold(int price)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (!CanAffordGold(price))
        {
            Debug.Log(
                $"[Wallet] Payment FAILED | Not enough Gold | Price={price}"
            );

            return false;
        }

        if (!ConvertAllToGold())
            return false;

        GoldOre -= price;

        Debug.Log(
            $"[Wallet] Payment SUCCESS | Spent={price} Gold | Remaining={GoldOre} Gold"
        );

        return true;
    }

    // Cộng Gold trực tiếp khi bán vật phẩm hoặc nhận thưởng.
    public bool TryAddGold(int amount)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (amount <= 0 || amount > int.MaxValue - GoldOre)
            return false;

        GoldOre += amount;

        Debug.Log(
            $"[Wallet] Received Gold: +{amount} | Total Gold: {GoldOre}"
        );

        return true;
    }

    // Cộng tài nguyên khi nhặt hoặc nhận thưởng.
    public bool TryAddResource(ResourceType type, int amount)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        switch (type)
        {
            case ResourceType.Wood:
                if (amount > int.MaxValue - Wood)
                    return false;

                Wood += amount;
                break;

            case ResourceType.CopperOre:
                if (amount > int.MaxValue - CopperOre)
                    return false;

                CopperOre += amount;
                break;

            case ResourceType.IronOre:
                if (amount > int.MaxValue - IronOre)
                    return false;

                IronOre += amount;
                break;

            case ResourceType.GoldOre:
                if (amount > int.MaxValue - GoldOre)
                    return false;

                GoldOre += amount;
                break;

            default:
                return false;
        }

        Debug.Log(
            $"[Wallet] Added Resource: {type} +{amount}\n" +
            $"Wood={Wood} | Copper={CopperOre} | " +
            $"Iron={IronOre} | Gold={GoldOre}"
        );

        return true;
    }
}
