using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
{
    private Vector2 currentInput;
    private bool isSprintingInput;

    private float mouseDeltaX;
    private float mouseDeltaY;

    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Spawned()
    {
        if (!HasInputAuthority) return;
        NetworkRunner runner = Runner;
        if (runner != null) runner.AddCallbacks(this);
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        currentInput = new Vector2(horizontal, vertical).normalized;

        isSprintingInput = Input.GetKey(KeyCode.LeftShift);

        // Lấy delta chuột thô của khung hình này
        mouseDeltaX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        mouseDeltaY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();
        data.movementInput = currentInput;
        data.isSprinting = isSprintingInput;

        // Truyền delta trực tiếp
        data.lookDeltaX = mouseDeltaX;
        data.lookDeltaY = mouseDeltaY;

        // Reset lại ngay lập tức để tránh lặp dữ liệu
        mouseDeltaX = 0f;
        mouseDeltaY = 0f;

        input.Set(data);
    }

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