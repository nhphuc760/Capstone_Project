using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BasicSpawner : SimulationBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
{
    [SerializeField] NetworkPrefabRef playerPrefabs;
    public static bool IsLoadingScene { get; private set; }

    NetworkInputData accumulatedInput;
    bool isReset = false;

    public void BeforeUpdate()
    {
        if (isReset)
        {
            isReset = false;
            accumulatedInput = default;
        }
        Keyboard curBoard = Keyboard.current;
        NetworkButtons button = default;
        if (curBoard != null)
        {
            Vector3 dirMove = Vector3.zero;

            if (curBoard.wKey.isPressed)
                dirMove.z += 1;
            if (curBoard.sKey.isPressed)
                dirMove.z += -1;
            if (curBoard.aKey.isPressed)
                dirMove.x += -1;
            if (curBoard.dKey.isPressed)
                dirMove.x += 1;
            accumulatedInput.moveDirection = dirMove.normalized;
            button.Set(ButtonType.Jump, curBoard.spaceKey.isPressed);
            button.Set(ButtonType.InteractMain, curBoard.eKey.isPressed);
            button.Set(ButtonType.EquipTool, curBoard.digit1Key.isPressed);
            button.Set(ButtonType.TestAddModify, curBoard.digit2Key.isPressed);
        }
        accumulatedInput.button = new NetworkButtons(accumulatedInput.button.Bits | button.Bits);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(accumulatedInput);
        isReset = true;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.Log("InputMissing");
    }


    public void OnSceneLoadStart(NetworkRunner runner)
    {
        IsLoadingScene = true;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        IsLoadingScene = false;

        if (!runner.IsServer || !IsGameplayScene()) return;

        foreach (var player in runner.ActivePlayers)
            SpawnPlayer(runner, player);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Ở lobby: không spawn. Chỉ spawn cho người vào muộn khi đang ở gameplay.
        if (runner.IsServer && !IsLoadingScene && IsGameplayScene())
            SpawnPlayer(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (runner.TryGetPlayerObject(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            runner.SetPlayerObject(player, null);
        }
    }

    void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (runner.TryGetPlayerObject(player, out _)) return; // tránh spawn trùng

        // TODO: thay bằng spawn point thật của scene gameplay
        Vector3 spawnPos = new Vector3(player.RawEncoded * 2f, 1f, 0f);

        NetworkObject obj = runner.Spawn(playerPrefabs, spawnPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, obj);
    }

    bool IsGameplayScene()
    {
        int gameplayIndex = SceneUtility.GetBuildIndexByScenePath(SceneDatabase.GAMEPLAY);
        return gameplayIndex >= 0 && SceneManager.GetActiveScene().buildIndex == gameplayIndex;
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        IsLoadingScene = false;
    }

  

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        EventBus<ReliableReceiveData>.Raise(new ReliableReceiveData { key = key, data = data.ToArray() });
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
}

public struct ReliableReceiveData : IEvent
{
    public ReliableKey key;
    public byte[] data;
}