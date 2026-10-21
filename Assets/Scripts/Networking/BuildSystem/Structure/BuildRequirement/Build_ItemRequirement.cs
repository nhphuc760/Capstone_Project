using System;
using Fusion;

[Serializable]
public class Build_ItemRequirement : EntityValueBuildRequirement
{

    public override BuildRequirementResult CheckRequirement(NetworkRunner runner, PlayerRef player)
    {
        NetworkInventory inventory = runner.GetPlayerObject(player).GetBehaviour<NetworkInventory>();
        if(values.Count == 0 ) return new BuildRequirementResult { Reason = BuildFailReason.MissingRequirement};
        foreach (var i in values)
        {
            ItemSO itemSO = i.dataSO as ItemSO;
            if (itemSO == null) continue;
            if (!inventory.HasItem(itemSO.ItemId, i.Value))
            {
                return new BuildRequirementResult
                {
                    Reason = BuildFailReason.MissingRequirement,
                    Message = $"Thiếu item {itemSO.ItemName}"
                };
            }
        }
        return default;
    }

    public override string GetInfo(PlayerRef player)
    {

        return null;
    }

    public override void OnBuildExecute(NetworkRunner runner, PlayerRef playerRef)
    {

        var inventory = runner.GetPlayerObject(playerRef).GetBehaviour<NetworkInventory>();
    }
}
