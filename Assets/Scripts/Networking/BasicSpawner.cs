using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasicSpawner : SimulationBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
{
    [SerializeField] NetworkPrefabRef playerPrefabs;
    NetworkInputData accumulatedInput;
    bool isReset = false;


    private void Awake()
    {
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {     
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        //string jsonToken = Encoding.UTF8.GetString(token);
        //var connectionToken = JsonConvert.DeserializeObject<ConnectionToken>(jsonToken);
        //RoomStatus status = RoomDatabaseManager.Instance.CurrentRoom.Status;
        //if(status != RoomStatus.Waiting && status != RoomStatus.Ready)
        //{
        //    request.Refuse();
        //    return;
        //}
        //else
        //{
        //    if (connectionToken != null)
        //    {
        //        request.Accept();
        //        Debug.Log("Chấp nhận yêu cầu từ:  " + connectionToken.userID);
        //    }
        //}       
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }


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
            Vector2 dirMove = Vector2.zero;

            if (curBoard.wKey.isPressed)
                dirMove.y += 1;
            if (curBoard.sKey.isPressed)
                dirMove.y += -1;
            if (curBoard.aKey.isPressed)
                dirMove.x += -1;
            if (curBoard.dKey.isPressed)
                dirMove.x += 1;
            accumulatedInput.moveDirection = dirMove.normalized;
            button.Set(ButtonType.Jump, curBoard.spaceKey.isPressed);
            button.Set(ButtonType.Interact, curBoard.eKey.isPressed);
            button.Set(ButtonType.EquipTool, curBoard.digit1Key.isPressed);
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

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //if (SessionManager.Ins != null && runner.IsServer)
        //{
        //    var token = runner.GetPlayerConnectionToken(player);
        //    string jsonToken = Encoding.UTF8.GetString(token);
        //    var obj = JsonConvert.DeserializeObject<ConnectionToken>(jsonToken);
        //    SessionManager.Ins._userIDs.Add(player, obj.userID);
        //}
        if (runner.IsServer)
        {
            runner.Spawn(playerPrefabs, new Vector3(10f, 1, 10f), Quaternion.identity, player, (runner, obj) => 
            {
                obj.name = player.ToString();
            });
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
      
    }

   
}
