using Fusion;

public struct NetworkItem : INetworkStruct
{
    public int itemID;
    public int quantity;

    public NetworkItem(int itemID, int quantity)
    {
        this.itemID = itemID;
        this.quantity = quantity;
    }
}