using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkInventory))]
public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local { get; private set; }

    public NetworkInventory Inventory { get; private set; }

    private void Awake()
    {
        Debug.Log("[NetworkPlayer] Awake");
    }

    public override void Spawned()
    {
        Debug.Log(
            $"[NetworkPlayer] Spawned - " +
            $"Object: {Object != null}, " +
            $"InputAuthority: {Object.InputAuthority}, " +
            $"HasInputAuthority: {Object.HasInputAuthority}, " +
            $"HasStateAuthority: {Object.HasStateAuthority}"
        );

        Inventory = GetComponent<NetworkInventory>();

        if (Inventory == null)
        {
            Debug.LogError(
                "[NetworkPlayer] Không tìm thấy NetworkInventory!"
            );

            return;
        }

        if (Object.HasInputAuthority)
        {
            Local = this;

            Debug.Log(
                "[NetworkPlayer] Local Player Registered"
            );
        }
        else
        {
            Debug.Log(
                "[NetworkPlayer] Đây không phải Local Player."
            );
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        Debug.Log("[NetworkPlayer] Despawned");

        if (Object.HasInputAuthority)
        {
            if (Local == this)
                Local = null;
        }
    }
}