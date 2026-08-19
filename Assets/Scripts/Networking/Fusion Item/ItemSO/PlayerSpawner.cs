using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private NetworkObject playerPrefab;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        NetworkObject obj = runner.Spawn(
            playerPrefab,
            Vector3.zero,
            Quaternion.identity,
            player
        );

        Debug.Log(
            $"Spawned {obj.name} | " +
            $"PlayerRef={player} | " +
            $"Object.InputAuthority={obj.InputAuthority} | " +
            $"HasInputAuthority={obj.HasInputAuthority} | " +
            $"HasStateAuthority={obj.HasStateAuthority}"
        );
    }
}