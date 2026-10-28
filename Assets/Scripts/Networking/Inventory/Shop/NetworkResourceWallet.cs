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

    [Header("Resource Exchange Rates")]
    [SerializeField, Min(1)] private int woodValue = 1;
    [SerializeField, Min(1)] private int copperOreValue = 5;
    [SerializeField, Min(1)] private int ironOreValue = 10;
    [SerializeField, Min(1)] private int goldOreValue = 50;

    public override void Spawned()
    {
        // Chỉ State Authority được khởi tạo dữ liệu mạng.
        if (!Object.HasStateAuthority)
            return;

        Wood = Mathf.Max(0, startingWood);
        CopperOre = Mathf.Max(0, startingCopperOre);
        IronOre = Mathf.Max(0, startingIronOre);
        GoldOre = Mathf.Max(0, startingGoldOre);
    }

    /// <summary>
    /// Tổng giá trị quy đổi của toàn bộ tài nguyên.
    /// </summary>
    public long GetTotalValue()
    {
        return (long)Wood * woodValue
             + (long)CopperOre * copperOreValue
             + (long)IronOre * ironOreValue
             + (long)GoldOre * goldOreValue;
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ giá trị để thanh toán hay không.
    /// </summary>
    public bool CanAfford(int price)
    {
        return price > 0 && GetTotalValue() >= price;
    }

    /// <summary>
    /// Thanh toán bằng tài nguyên.
    /// Nếu trả dư, phần chênh lệch được hoàn lại dưới dạng Wood.
    /// Chỉ State Authority được phép thay đổi tài nguyên.
    /// </summary>
    public bool TrySpendValue(int price)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (price <= 0 || GetTotalValue() < price)
            return false;

        int remaining = price;
        long paidValue = 0;

        int useGold = Mathf.Min(GoldOre, DivideRoundUp(remaining, goldOreValue));
        GoldOre -= useGold;
        remaining -= useGold * goldOreValue;
        paidValue += (long)useGold * goldOreValue;

        int useIron = Mathf.Min(IronOre, DivideRoundUp(remaining, ironOreValue));
        IronOre -= useIron;
        remaining -= useIron * ironOreValue;
        paidValue += (long)useIron * ironOreValue;

        int useCopper = Mathf.Min(CopperOre, DivideRoundUp(remaining, copperOreValue));
        CopperOre -= useCopper;
        remaining -= useCopper * copperOreValue;
        paidValue += (long)useCopper * copperOreValue;

        int useWood = Mathf.Min(Wood, DivideRoundUp(remaining, woodValue));
        Wood -= useWood;
        remaining -= useWood * woodValue;
        paidValue += (long)useWood * woodValue;

        // Nếu vẫn còn thiếu, không thể thanh toán.
        // Trường hợp này được kiểm tra trước bằng GetTotalValue,
        // nhưng vẫn giữ kiểm tra an toàn.
        if (remaining > 0)
        {
            GoldOre += useGold;
            IronOre += useIron;
            CopperOre += useCopper;
            Wood += useWood;
            return false;
        }

        long change = paidValue - price;
        long returnedWood = change / woodValue;

        if (returnedWood > int.MaxValue - (long)Wood)
        {
            // Hoàn nguyên giao dịch nếu số Wood bị tràn int.
            GoldOre += useGold;
            IronOre += useIron;
            CopperOre += useCopper;
            Wood += useWood;
            return false;
        }

        Wood += (int)returnedWood;
        return true;
    }

    /// <summary>
    /// Cộng thêm tài nguyên. Dùng khi nhặt tài nguyên hoặc nhận thưởng.
    /// </summary>
    public bool TryAddResource(ResourceType type, int amount)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        switch (type)
        {
            case ResourceType.Wood:
                if (amount > int.MaxValue - Wood) return false;
                Wood += amount;
                return true;

            case ResourceType.CopperOre:
                if (amount > int.MaxValue - CopperOre) return false;
                CopperOre += amount;
                return true;

            case ResourceType.IronOre:
                if (amount > int.MaxValue - IronOre) return false;
                IronOre += amount;
                return true;

            case ResourceType.GoldOre:
                if (amount > int.MaxValue - GoldOre) return false;
                GoldOre += amount;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Bán vật phẩm cho Shop: cộng phần tiền bán dưới dạng Wood.
    /// </summary>
    public bool TryAddValueAsWood(int value)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (value <= 0)
            return false;

        if (value % woodValue != 0)
            return false;

        int woodToAdd = value / woodValue;

        if (woodToAdd > int.MaxValue - Wood)
            return false;

        Wood += woodToAdd;
        return true;
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        if (value <= 0)
            return 0;

        return (value + divisor - 1) / divisor;
    }
}