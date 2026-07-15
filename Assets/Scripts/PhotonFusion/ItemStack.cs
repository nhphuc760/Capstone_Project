using Fusion;

public struct ItemStack : INetworkStruct
{
    public int ItemID;

    public ushort Amount;

    // Nếu sau này cần độ bền
    public ushort Durability;
}