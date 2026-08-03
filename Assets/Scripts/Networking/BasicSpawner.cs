using System;
using System.Collections.Generic;
using System.Text;
using Fusion;
using Fusion.Sockets;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private NetworkPrefabRef playerPrefab;
    private NetworkRunner runner;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private void Start()
    {
        StartGame();
    }

    private async void StartGame()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Single,
            SessionName = "MovingOutTestRoom",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Trong Fusion, chỉ Server/Host mới có quyền Instantiate vật thể có đồng bộ mạng
        if (runner.IsServer)
        {
            // Random toạ độ một chút để 2 người vào không bị kẹt dính lấy nhau
            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-2f, 2f), 5f, UnityEngine.Random.Range(-2f, 2f));

            // Spawn Prefab xuống Scene
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // Lưu vào Dictionary để quản lý
            spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Tìm xem người chơi vừa thoát có nhân vật trên Scene không
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            // Xoá nhân vật đó khỏi mạng lưới và gỡ khỏi Dictionary
            runner.Despawn(networkObject);
            spawnedCharacters.Remove(player);
        }
    }


    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        string jsonToken = Encoding.UTF8.GetString(token);
        var connectionToken = JsonConvert.DeserializeObject<ConnectionToken>(jsonToken);
        if (connectionToken != null)
        {
            request.Accept();
            Debug.Log("Chấp nhận yêu cầu từ:  " + connectionToken.userID);
        }
    }

    // --- CÁC HÀM CÒN LẠI ĐỂ TRỐNG ---

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}