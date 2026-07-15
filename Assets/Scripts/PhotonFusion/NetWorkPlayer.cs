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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (NetworkPlayer.Local == null)
            {
                Debug.Log("Local Player = NULL");
                return;
            }

            Debug.Log($"Local Player : {NetworkPlayer.Local.name}");
            Debug.Log($"Inventory : {NetworkPlayer.Local.Inventory}");
        }
    }

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