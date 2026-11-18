using Fusion;
using UnityEngine;
public class ItemToolKitUse : NetworkBehaviour
{
    [SerializeField] NetworkInventory inventory;  


    public bool ServerUse(int hotBarIndex)
    {
        if (!HasStateAuthority || hotBarIndex >= NetworkInventory.HotbarCapacity) return false;

        var itemSO = inventory.GetHotbarItemData(hotBarIndex);
        if(itemSO == null || itemSO.Components.Count == 0) return false;
        var ctx = MakeContext(hotBarIndex);
        foreach (var i in itemSO.Components) 
        {
            if (!i.CanUse(ctx)) return false;
        }
        foreach (var c in itemSO.Components)
        {
            c?.OnUse(ctx);
        }
        if(ctx.ConsumeAmount > 0)
        {
            inventory.RemoveItem(itemSO.ItemId, ctx.ConsumeAmount);
        }
        return true;

    }






    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUse(int hotBarIndex)
    {
        ServerUse(hotBarIndex);
    }



    ItemContext MakeContext(int hotBarIndex)
    {
        ItemSO def = inventory.GetHotbarItemData(hotBarIndex);
        return new ItemContext(Object, inventory, def, hotBarIndex);
    }
}
