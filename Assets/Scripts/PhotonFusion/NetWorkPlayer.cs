using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkInventory))]
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>
    /// Player của máy hiện tại.
    /// </summary>
    public static NetworkPlayer Local { get; private set; }

    /// <summary>
    /// Inventory của Player.
    /// </summary>
    public NetworkInventory Inventory { get; private set; }

    public override void Spawned()
    {
        Inventory = GetComponent<NetworkInventory>();

        if (Object.HasInputAuthority)
        {
            Local = this;

            Debug.Log("[NetworkPlayer] Local Player Registered");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority)
        {
            if (Local == this)
                Local = null;
        }
    }
}