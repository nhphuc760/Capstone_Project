using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using Cysharp.Threading.Tasks;

public class LobbyUIManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Room Code Corner UI")]
    [SerializeField] private TextMeshProUGUI txtRoomCode;
    [SerializeField] private Button btnCopyRoomCode;
    [SerializeField] private TextMeshProUGUI txtPlayerCount;
    [SerializeField] private GameObject roomCodeContainer;

    [Header("Lobby Controls")]
    [SerializeField] private Button btnLeaveRoom;
    [SerializeField] private string chooseTypeSceneName = "ChooseType";

    private NetworkRunner activeRunner;
    private string currentCode = "";
    private bool isLeavingLobby;

    public static bool IsSinglePlaySession(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        if (runner.GameMode == GameMode.Single)
            return true;

        if (runner.SessionInfo.IsValid && runner.SessionInfo.MaxPlayers == 1)
            return true;

        return false;
    }

    private async void Start()
    {
        activeRunner = FindFirstObjectByType<NetworkRunner>();
        if (activeRunner != null)
        {
            activeRunner.RemoveCallbacks(this);
            activeRunner.AddCallbacks(this);
        }

        if (btnCopyRoomCode != null)
            btnCopyRoomCode.onClick.AddListener(() => CopyRoomCodeAsync().Forget());

        if (btnLeaveRoom != null)
            btnLeaveRoom.onClick.AddListener(() => LeaveLobbyAsync().Forget());

        // Cập nhật trạng thái InParty để bạn bè ngoài ChooseType thấy "In Lobby" và nút Request
        if (!IsSinglePlaySession(activeRunner) && NetworkDataManager.Instance != null)
        {
            NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.InParty).Forget();
        }

        RefreshLobbyDisplay();

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeOut()
                                          .Perform();
        }

        RefreshLobbyDisplay();
    }

    private void OnDestroy()
    {
        if (activeRunner != null)
            activeRunner.RemoveCallbacks(this);
    }

    public void RefreshLobbyDisplay()
    {
        if (activeRunner == null)
        {
            activeRunner = FindFirstObjectByType<NetworkRunner>();
            if (activeRunner != null)
            {
                activeRunner.RemoveCallbacks(this);
                activeRunner.AddCallbacks(this);
            }
        }

        bool isSingle = IsSinglePlaySession(activeRunner);

        if (isSingle)
        {
            currentCode = "";
            if (txtPlayerCount != null)
                txtPlayerCount.text = "Players: 1/1";

            if (roomCodeContainer != null)
                roomCodeContainer.SetActive(false);

            if (txtRoomCode != null)
                txtRoomCode.gameObject.SetActive(false);

            if (btnCopyRoomCode != null)
                btnCopyRoomCode.gameObject.SetActive(false);

            return;
        }

        if (roomCodeContainer != null)
            roomCodeContainer.SetActive(true);
        if (txtRoomCode != null)
            txtRoomCode.gameObject.SetActive(true);
        if (btnCopyRoomCode != null)
            btnCopyRoomCode.gameObject.SetActive(true);

        if (activeRunner != null && activeRunner.SessionInfo.IsValid)
        {
            currentCode = !string.IsNullOrEmpty(activeRunner.SessionInfo.Name)
                ? activeRunner.SessionInfo.Name
                : (RoomDatabaseManager.Instance?.CurrentRoom?.RoomName ?? PlayerPrefs.GetString("CurrentRoomID", "-----"));

            int count = activeRunner.ActivePlayers.Count();
            int max = activeRunner.SessionInfo.MaxPlayers > 0
                ? activeRunner.SessionInfo.MaxPlayers
                : 4;

            if (txtPlayerCount != null)
                txtPlayerCount.text = $"Players: {count}/{max}";
        }
        else
        {
            currentCode = PlayerPrefs.GetString("CurrentRoomID", "-----");
            if (txtPlayerCount != null)
                txtPlayerCount.text = "Players: 1/4";
        }

        if (txtRoomCode != null)
            txtRoomCode.text = $"ROOM: {currentCode}";
    }

    private async UniTaskVoid CopyRoomCodeAsync()
    {
        if (string.IsNullOrEmpty(currentCode) || currentCode == "-----") return;

        GUIUtility.systemCopyBuffer = currentCode;
        if (txtRoomCode != null)
            txtRoomCode.text = $"COPIED: {currentCode}";

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f), ignoreTimeScale: true);

        if (this != null && txtRoomCode != null)
            txtRoomCode.text = $"ROOM: {currentCode}";
    }

    private async UniTaskVoid LeaveLobbyAsync()
    {
        if (isLeavingLobby) return;
        isLeavingLobby = true;

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeIn()
                                          .Perform();
        }

        if (RoomDatabaseManager.Instance != null)
            await RoomDatabaseManager.Instance.LeaveRoom();

        if (NetworkDataManager.Instance != null)
            await NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.Online);

        if (activeRunner == null)
            activeRunner = FindFirstObjectByType<NetworkRunner>();

        if (activeRunner != null)
        {
            activeRunner.RemoveCallbacks(this);
            if (activeRunner.IsRunning)
            {
                try { await activeRunner.Shutdown(); }
                catch { }
            }
            Destroy(activeRunner.gameObject);
            activeRunner = null;
        }

        await SceneManager.LoadSceneAsync(chooseTypeSceneName, LoadSceneMode.Single);
        await UniTask.Yield();

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeOut()
                                          .Perform();
        }
    }

    #region INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => RefreshLobbyDisplay();
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => RefreshLobbyDisplay();
    public void OnConnectedToServer(NetworkRunner runner) => RefreshLobbyDisplay();
    public void OnSceneLoadDone(NetworkRunner runner) => RefreshLobbyDisplay();
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => LeaveLobbyAsync().Forget();

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}