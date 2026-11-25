using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Cysharp.Threading.Tasks;

public class ModeSelectCardManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Photon Fusion")]
    [SerializeField] private FusionBootstrap fusionBootstrap;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [Tooltip("Khóa cố định Region Photon (vd: asia, jp, sg) để Host và Client luôn gặp nhau sau khi rời trận.")]
    [SerializeField] private string defaultPhotonRegion = "asia";

    [Header("Cards")]
    [SerializeField] private ModeCardItem[] modeCards;

    [Header("Bottom Description UI")]
    [SerializeField] private TextMeshProUGUI txtDescTitle;
    [SerializeField] private TextMeshProUGUI txtDescMain;
    [SerializeField] private TextMeshProUGUI txtDescHighlight;

    [Header("Join Room Input Area")]
    [SerializeField] private GameObject joinInputBar;
    [SerializeField] private TMP_InputField inputRoomId;

    [Header("Error Notification Panel")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI txtErrorTitle;
    [SerializeField] private TextMeshProUGUI txtErrorMessage;
    [SerializeField] private Button btnCloseError;
    [SerializeField] private float connectTimeoutSeconds = 12f;

    private ModeCardItem joinCardRef;
    private NetworkRunner trackedRunner;
    private bool isConnecting;
    private const string ROOM_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private void OnEnable()
    {
        isConnecting = false;
        trackedRunner = null;
    }

    private void Start()
    {
        if (fusionBootstrap == null || fusionBootstrap.RunnerPrefab == null)
            fusionBootstrap = FindValidFusionBootstrap();

        if (fusionBootstrap != null)
        {
            fusionBootstrap.StartMode = FusionBootstrap.StartModes.Manual;
            if (fusionBootstrap.TryGetComponent<FusionBootstrapDebugGUI>(out var debugGui))
                debugGui.enabled = false;
        }

        if (joinInputBar != null) joinInputBar.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);

        if (btnCloseError != null)
            btnCloseError.onClick.AddListener(CloseErrorPanel);

        foreach (var card in modeCards)
        {
            if (card == null) continue;
            card.OnCardHovered += HandleCardHovered;
            card.OnCardClicked += HandleCardClicked;

            if (card.modeType == ModeCardItem.GameModeOption.JoinRoom)
                joinCardRef = card;
        }

        if (modeCards != null && modeCards.Length > 0 && modeCards[0] != null)
        {
            UpdateDescriptionPanel(modeCards[0]);
        }

        if (inputRoomId != null)
        {
            inputRoomId.onValueChanged.AddListener(val =>
            {
                string upper = val.ToUpper();
                if (inputRoomId.text != upper) inputRoomId.text = upper;
            });

            inputRoomId.onSubmit.AddListener(_ => SubmitJoinRoom());
        }
    }

    private void OnDestroy()
    {
        if (trackedRunner != null)
            trackedRunner.RemoveCallbacks(this);
    }

    private void Update()
    {
        if (errorPanel != null && errorPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                CloseErrorPanel();
            }
            return;
        }

        if (joinInputBar != null && joinInputBar.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideJoinInputBar();
        }
    }

    private FusionBootstrap FindValidFusionBootstrap()
    {
        var bootstraps = FindObjectsByType<FusionBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bootstraps.Length; i++)
        {
            if (bootstraps[i] != null && bootstraps[i].RunnerPrefab != null)
                return bootstraps[i];
        }
        return bootstraps.Length > 0 ? bootstraps[0] : null;
    }

    private void HandleCardHovered(ModeCardItem hoveredCard)
    {
        UpdateDescriptionPanel(hoveredCard);
    }

    private void UpdateDescriptionPanel(ModeCardItem card)
    {
        if (txtDescTitle != null) txtDescTitle.text = card.modeTitle;
        if (txtDescMain != null) txtDescMain.text = card.mainDescription;
        if (txtDescHighlight != null) txtDescHighlight.text = card.highlightDescription;
    }

    private void HandleCardClicked(ModeCardItem clickedCard)
    {
        if (isConnecting) return;
        if (errorPanel != null && errorPanel.activeSelf) return;

        switch (clickedCard.modeType)
        {
            case ModeCardItem.GameModeOption.SinglePlayer:
                HideJoinInputBar();
                StartFusionSession(GameMode.Single, "SINGLE_" + GenerateRoomCode(5)).Forget();
                break;

            case ModeCardItem.GameModeOption.HostCoop:
                HideJoinInputBar();
                string roomCode = GenerateRoomCode(5);
                StartFusionSession(GameMode.Host, roomCode).Forget();
                break;

            case ModeCardItem.GameModeOption.JoinRoom:
                ShowJoinInputBar();
                break;
        }
    }

    private void ShowJoinInputBar()
    {
        if (joinCardRef != null) joinCardRef.SetLockedHighlight(true);

        if (joinInputBar != null)
        {
            joinInputBar.SetActive(true);
            if (inputRoomId != null)
            {
                inputRoomId.text = "";
                inputRoomId.Select();
                inputRoomId.ActivateInputField();
            }
        }
    }

    private void HideJoinInputBar()
    {
        if (joinCardRef != null) joinCardRef.SetLockedHighlight(false);
        if (joinInputBar != null) joinInputBar.SetActive(false);
    }

    private void SubmitJoinRoom()
    {
        if (isConnecting) return;
        if (errorPanel != null && errorPanel.activeSelf) return;

        string code = inputRoomId != null ? inputRoomId.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code))
        {
            ShowErrorPanel("INVALID CODE", "Please enter a valid room code before joining.");
            return;
        }

        StartFusionSession(GameMode.Client, code).Forget();
    }

    public void HostRoomByInvite(string roomCode)
    {
        if (isConnecting || string.IsNullOrWhiteSpace(roomCode)) return;
        HideJoinInputBar();
        StartFusionSession(GameMode.Host, roomCode.Trim().ToUpper()).Forget();
    }

    public void JoinRoomByInvite(string roomCode)
    {
        if (isConnecting || string.IsNullOrWhiteSpace(roomCode)) return;
        HideJoinInputBar();
        StartFusionSession(GameMode.Client, roomCode.Trim().ToUpper()).Forget();
    }

    private async UniTaskVoid StartFusionSession(GameMode mode, string sessionName)
    {
        if (fusionBootstrap == null || fusionBootstrap.RunnerPrefab == null)
            fusionBootstrap = FindValidFusionBootstrap();

        if (fusionBootstrap == null || fusionBootstrap.RunnerPrefab == null)
        {
            Debug.LogError("[ModeSelect] Không tìm thấy FusionBootstrap hoặc RunnerPrefab hợp lệ!");
            return;
        }

        isConnecting = true;
        sessionName = sessionName.Trim().ToUpper();

        // 1. Dọn sạch toàn bộ NetworkRunner cũ và đợi hoàn tất trước khi tạo phiên mới
        await CleanupStaleRunnersAsync();

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeIn()
                                          .Perform();
        }

        // 2. Đồng bộ Firebase & khóa cứng Region Photon
        string targetRegion = string.IsNullOrWhiteSpace(defaultPhotonRegion) ? "asia" : defaultPhotonRegion.Trim().ToLower();
        if (mode == GameMode.Client)
        {
            string hostRegion = await SyncFirebaseRoomAsync(isHost: false, sessionName);
            if (!string.IsNullOrEmpty(hostRegion))
                targetRegion = hostRegion.Trim().ToLower();
        }
        else if (mode == GameMode.Host)
        {
            await SyncFirebaseRoomAsync(isHost: true, sessionName, targetRegion);
        }

        FusionAppSettings customAppSettings = BuildFixedRegionSettings(targetRegion);

        PlayerPrefs.SetString("CurrentRoomID", sessionName);
        PlayerPrefs.Save();

        // 3. Khởi tạo trực tiếp NetworkRunner mới (không phụ thuộc state cũ của FusionBootstrap)
        NetworkRunner runner = Instantiate(fusionBootstrap.RunnerPrefab);
        runner.name = $"NetworkRunner_{mode}_{sessionName}";
        runner.ProvideInput = true;
        trackedRunner = runner;

        runner.RemoveCallbacks(this);
        runner.AddCallbacks(this);

        int lobbyBuildIndex = SceneUtility.GetBuildIndexByScenePath(lobbySceneName);
        if (lobbyBuildIndex < 0)
            lobbyBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;

        INetworkSceneManager sceneManager = runner.GetComponent<INetworkSceneManager>()
                                            ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startArgs = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(lobbyBuildIndex),
            SceneManager = sceneManager,
            PlayerCount = mode == GameMode.Single ? 1 : 4,
            CustomPhotonAppSettings = customAppSettings
        };

        Debug.Log($"[ModeSelect] Bắt đầu StartGame -> Mode: {mode} | Room Code: {sessionName} | FixedRegion: {targetRegion}");

        StartGameResult startResult;
        try
        {
            startResult = await runner.StartGame(startArgs);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModeSelect] Ngoại lệ StartGame ({sessionName}): {ex.Message}");
            await HandleSessionFailedAsync("CONNECTION ERROR", "Unable to start network session. Please try again.");
            return;
        }

        if (!isConnecting) return;

        if (!startResult.Ok)
        {
            Debug.LogError($"[ModeSelect] Kết nối thất bại tới phòng '{sessionName}' (Mode: {mode}, Reason: {startResult.ShutdownReason}).");
            string title = mode == GameMode.Client ? "ROOM NOT FOUND" : "HOST FAILED";
            string msg = startResult.ShutdownReason == ShutdownReason.GameNotFound
                ? $"Room \"{sessionName}\" does not exist or has already closed."
                : $"Unable to connect ({startResult.ShutdownReason}).";

            await HandleSessionFailedAsync(title, msg);
            return;
        }

        var info = runner.SessionInfo;
        string actualRegion = !string.IsNullOrEmpty(info.Region) ? info.Region.Trim().ToLower() : targetRegion;
        Debug.Log($"[ModeSelect] Kết nối thành công -> Mode: {mode} | Room: {info.Name} | Region: {actualRegion} | Players: {info.PlayerCount}/{info.MaxPlayers}");

        if (mode == GameMode.Host)
        {
            await SyncFirebaseRoomAsync(isHost: true, sessionName, actualRegion);
        }

        isConnecting = false;
    }

    private FusionAppSettings BuildFixedRegionSettings(string region)
    {
        FusionAppSettings copy = PhotonAppSettings.Global != null && PhotonAppSettings.Global.AppSettings != null
            ? PhotonAppSettings.Global.AppSettings.GetCopy()
            : new FusionAppSettings();

        copy.FixedRegion = region;

        if (PhotonAppSettings.Global != null && PhotonAppSettings.Global.AppSettings != null)
            PhotonAppSettings.Global.AppSettings.FixedRegion = region;

        return copy;
    }

    private async UniTask CleanupStaleRunnersAsync()
    {
        var runnerSet = new HashSet<NetworkRunner>(
            FindObjectsByType<NetworkRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        foreach (var activeInstance in NetworkRunner.Instances.ToList())
        {
            if (activeInstance != null)
                runnerSet.Add(activeInstance);
        }

        bool destroyedAny = false;
        foreach (var r in runnerSet)
        {
            if (r == null) continue;

            r.RemoveCallbacks(this);
            if (!r.IsShutdown)
            {
                try { await r.Shutdown(); }
                catch { /* Bỏ qua nếu runner đã đóng */ }
            }
            Destroy(r.gameObject);
            destroyedAny = true;
        }

        trackedRunner = null;

        if (destroyedAny)
            await UniTask.DelayFrame(2);
    }

    private async UniTask HandleSessionFailedAsync(string title, string message)
    {
        if (!isConnecting) return;
        isConnecting = false;

        if (trackedRunner != null)
        {
            trackedRunner.RemoveCallbacks(this);
            if (!trackedRunner.IsShutdown)
            {
                try { await trackedRunner.Shutdown(); }
                catch { }
            }
            Destroy(trackedRunner.gameObject);
            trackedRunner = null;
        }

        if (RoomDatabaseManager.Instance != null && RoomDatabaseManager.Instance.CurrentRoom != null)
        {
            await RoomDatabaseManager.Instance.LeaveRoom();
        }

        if (SceneController.Instance != null && SceneController.Instance.loadingOverlay != null)
        {
            await SceneController.Instance.loadingOverlay.FadeOutBlack(0.3f);
        }

        ShowErrorPanel(title, message);
    }

    public void ShowErrorPanel(string title, string message)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (txtErrorTitle != null) txtErrorTitle.text = title;
        if (txtErrorMessage != null) txtErrorMessage.text = message;

        if (errorPanel != null)
            errorPanel.SetActive(true);
    }

    public void CloseErrorPanel()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);

        if (joinInputBar != null && joinInputBar.activeSelf && inputRoomId != null)
        {
            inputRoomId.Select();
            inputRoomId.ActivateInputField();
        }
    }

    private async UniTask<string> SyncFirebaseRoomAsync(bool isHost, string roomCode, string activeRegion = null)
    {
        if (RoomDatabaseManager.Instance == null || FirebaseManager.RealtimeDB.reference == null)
            return null;

        try
        {
            if (isHost)
            {
                Room room = RoomDatabaseManager.Instance.CurrentRoom;
                if (room == null || room.RoomName != roomCode || room.HostID != FirebaseManager.UserID)
                {
                    room = await RoomDatabaseManager.Instance.CreateRoom(roomCode, 4);
                }
                if (room != null)
                {
                    await RoomDatabaseManager.Instance.UpdateStatus(RoomStatus.Ready);
                    await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomCode}").SetValueAsync(room.RoomID);
                    if (!string.IsNullOrEmpty(activeRegion))
                    {
                        await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{room.RoomID}/Region").SetValueAsync(activeRegion);
                    }
                }
                return activeRegion;
            }
            else
            {
                var snap = await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomCode}").GetValueAsync();
                if (snap.Exists && snap.Value != null)
                {
                    string roomId = snap.Value.ToString();
                    await RoomDatabaseManager.Instance.JoinRoom(roomId);
                    await RoomDatabaseManager.Instance.AddMembers(FirebaseManager.UserID);

                    var regionSnap = await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomId}/Region").GetValueAsync();
                    if (regionSnap.Exists && regionSnap.Value != null)
                        return regionSnap.Value.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ModeSelect] Bỏ qua đồng bộ Firebase: " + e.Message);
        }

        return null;
    }

    private string GenerateRoomCode(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = ROOM_CHARS[UnityEngine.Random.Range(0, ROOM_CHARS.Length)];
        return new string(chars);
    }

    #region INetworkRunnerCallbacks
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (!isConnecting || runner != trackedRunner) return;

        Debug.LogError($"[ModeSelect] OnShutdown với lý do: {shutdownReason}");
        string msg = shutdownReason == ShutdownReason.GameNotFound
            ? "Room does not exist. Please check your room code."
            : $"Unable to join room ({shutdownReason}).";

        HandleSessionFailedAsync("JOIN ROOM FAILED", msg).Forget();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (!isConnecting || runner != trackedRunner) return;
        HandleSessionFailedAsync("CONNECTION FAILED", $"Could not connect to server ({reason}).").Forget();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}