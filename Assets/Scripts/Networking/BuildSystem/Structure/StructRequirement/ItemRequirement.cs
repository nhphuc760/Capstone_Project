using System;
using Fusion;

[Serializable]
public class ItemRequirement : RequirementDefinition
{
    public ItemSO itemSO;
    public int _quantity;
    public NetworkInventory inventory { get; set; }

    public override IRequirement CreateRuntime(RequirementServices services, NetworkObject source = default)
    {
        return new ItemRequirementInstance
        {
            definition = this,
            networkInventory = services.inventory
        };
    }

   
}



[Serializable]
public class ItemRequirementInstance : IRequirement
{
    public ItemRequirement definition;
    public NetworkInventory networkInventory;

    public event Action Changed;

    public void Dispose()
    {

    }

    public void Effect()
    {
        networkInventory.RemoveItem(definition.itemSO.ItemId, definition._quantity);
    }

    public string GetInfor()
    {
        return $"<sprite item> x{definition._quantity}";
    }

    public CheckMetResult Met()
    {
        if (!networkInventory.HasItem(definition.itemSO.ItemId, definition._quantity))
        {
            return new CheckMetResult
            {
                Reason = CheckMetReason.NotEnoughResource,
                Message = "Không đủ vật phẩm"
            };
        }
        return default;
    }

    public void Start()
    {

    }
}