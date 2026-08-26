using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private NetworkObject playerPrefab;

    public void OnPlayerJoined(
    NetworkRunner runner,
    PlayerRef player)
{
    Debug.Log(
        $"[{runner.GameMode}] OnPlayerJoined: {player} | " +
        $"IsServer={runner.IsServer} | " +
        $"IsClient={runner.IsClient}"
    );

    if (!runner.IsServer)
    {
        Debug.Log(
            $"[{runner.GameMode}] Client detected PlayerJoined: {player}"
        );

        return;
    }

    NetworkObject playerObject =
        runner.Spawn(
            playerPrefab,
            Vector3.zero,
            Quaternion.identity,
            player
        );

    runner.SetPlayerObject(
        player,
        playerObject
    );

    Debug.Log(
        $"[{runner.GameMode}] Spawned {playerObject.name} | " +
        $"PlayerRef={player} | " +
        $"InputAuthority={playerObject.InputAuthority} | " +
        $"HasInputAuthority={playerObject.HasInputAuthority} | " +
        $"HasStateAuthority={playerObject.HasStateAuthority}"
    );
}
}