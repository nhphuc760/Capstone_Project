using Fusion;
using UnityEngine;

public enum Mode
{
    PersonalInv
}

public class InventoryMode : NetworkBehaviour
{
    [Networked]
    public Mode mode { get; private set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            mode = Mode.PersonalInv;
        }
    }

    public NetworkInventory GetActiveInventory(
        NetworkObject playerObject)
    {
        if (playerObject == null)
            return null;

        return playerObject.GetComponent<NetworkInventory>();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_SplitInventory()
    {
        mode = Mode.PersonalInv;

        Debug.Log(
            "Inventory is already PERSONAL."
        );
    }
}
