using Fusion;
using Fusion.Sockets;
using UnityEngine;
using static Unity.Collections.Unicode;

public class PlayerInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
{
    private Vector2 currentInput;
    private bool isSprintingInput;

    public override void Spawned()
    {
        // Đảm bảo chỉ có máy sở hữu nhân vật này mới lắng nghe phím từ bàn phím của mình
        if (!HasInputAuthority) return;

        // Tự động đăng ký nhận callback từ NetworkRunner hiện tại
        NetworkRunner runner = Runner;
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
    }

    private void Update()
    {
        // Chỉ đọc phím nếu đây là nhân vật của chính mình
        if (!HasInputAuthority) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        currentInput = new Vector2(horizontal, vertical).normalized;
        isSprintingInput = Input.GetKey(KeyCode.LeftShift);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();
        data.movementInput = currentInput;
        data.isSprinting = isSprintingInput;
        input.Set(data);
    }

    //CÁC HÀM CALLBACK KHÁC CỦA FUSION (Để trống tạm thời)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}