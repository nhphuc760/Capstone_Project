using Fusion;

public struct SharedInventoryItem : INetworkStruct
{
    public int itemID;
    public int amount;

    // Player đã lấy item này
    public PlayerRef owner;

    public SharedInventoryItem(
        int itemID,
        int amount,
        PlayerRef owner)
    {
        this.itemID = itemID;
        this.amount = amount;
        this.owner = owner;
    }

    public bool IsEmpty =>
        itemID == 0 || amount <= 0;
}