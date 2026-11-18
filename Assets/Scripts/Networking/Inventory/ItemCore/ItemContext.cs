using Fusion;

public sealed class ItemContext
{
    public NetworkObject User { get; }
    public NetworkInventory Inventory { get; }
    public ItemSO Definition { get; }
    public int SlotIndex { get; }


    /// <summary>Component đặt >0 để yêu cầu trừ số lượng sau khi dùng (Consumable đặt = 1).</summary>
    public int ConsumeAmount { get; set; }
    public bool IsServer => User != null && User.Runner != null && User.Runner.IsServer;
    public NetworkRunner Runner => User.Runner;
    public ItemContext(NetworkObject user, NetworkInventory inventory,
                       ItemSO def, int slotIndex)
    {
        User = user;
        Inventory = inventory;
        Definition = def;
        SlotIndex = slotIndex;
    }

    /// <summary>Lấy hệ thống bất kỳ trên người dùng (IStatSink, Health, ...) qua interface.</summary>
    public T Get<T>() where T : class
    {
        return User != null ? User.GetComponentInChildren<T>(true) : null;
    }
}