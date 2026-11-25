using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkInputData _accumulatedInput;
    private bool _isReset;

    private void Update()
    {
        if (GameplayPauseManager.IsPaused)
    {
        _accumulatedInput = default;
        return;
    }
        if (_isReset)
        {
            _isReset = false;
            _accumulatedInput = default;
        }

        Keyboard curBoard = Keyboard.current;
        NetworkButtons button = default;
        if (curBoard != null)
        {
            Vector3 dirMove = Vector3.zero;
            if (curBoard.wKey.isPressed) dirMove.z += 1;
            if (curBoard.sKey.isPressed) dirMove.z -= 1;
            if (curBoard.aKey.isPressed) dirMove.x -= 1;
            if (curBoard.dKey.isPressed) dirMove.x += 1;

            _accumulatedInput.moveDirection = dirMove.normalized;
            button.Set(ButtonType.Jump, curBoard.spaceKey.isPressed);
            button.Set(ButtonType.InteractMain, curBoard.eKey.isPressed);
            button.Set(ButtonType.EquipTool, curBoard.digit1Key.isPressed);
            button.Set(ButtonType.TestAddModify, curBoard.digit2Key.isPressed);
        }

        _accumulatedInput.button = new NetworkButtons(_accumulatedInput.button.Bits | button.Bits);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(_accumulatedInput);
        _isReset = true;
    }

    // Các callback bắt buộc còn lại của INetworkRunnerCallbacks
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}