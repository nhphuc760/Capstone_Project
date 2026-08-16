using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkInventory))]
public class NetworkItem : NetworkBehaviour
{
    public static NetworkItem Local { get; private set; }

    public NetworkInventory Inventory { get; private set; }

    private void Awake()
    {
        Debug.Log("[NetworkItem] Awake");
    }

    public override void Spawned()
    {
        Debug.Log(
            $"[NetworkItem] Spawned - " +
            $"Object: {Object != null}, " +
            $"InputAuthority: {Object.InputAuthority}, " +
            $"HasInputAuthority: {Object.HasInputAuthority}, " +
            $"HasStateAuthority: {Object.HasStateAuthority}"
        );

        Inventory = GetComponent<NetworkInventory>();

        if (Inventory == null)
        {
            Debug.LogError(
                "[NetworkItem] Không tìm thấy NetworkInventory!"
            );

            return;
        }

        if (Object.HasInputAuthority)
        {
            Local = this;

            Debug.Log(
                "[NetworkItem] Local Player Registered"
            );
        }
        else
        {
            Debug.Log(
                "[NetworkItem] Đây không phải Local Player."
            );
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        Debug.Log("[NetworkItem] Despawned");

        if (Object.HasInputAuthority)
        {
            if (Local == this)
                Local = null;
        }
    }
}