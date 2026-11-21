using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Fusion;
using Fusion.Sockets;
using Cysharp.Threading.Tasks;

public class ModeSelectCardManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Photon Fusion")]
    [SerializeField] private FusionBootstrap fusionBootstrap;

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
    [SerializeField] private float connectTimeoutSeconds = 8f;

    private ModeCardItem joinCardRef;
    private NetworkRunner trackedRunner;
    private bool isConnecting;
    private const string ROOM_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private void Start()
    {
        if (fusionBootstrap == null)
            fusionBootstrap = FindAnyObjectByType<FusionBootstrap>();

        // Ép chế độ Manual để ẩn UI debug mặc định của FusionBootstrap
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
        // Ưu tiên đóng Panel báo lỗi trước nếu đang mở
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

    private async UniTaskVoid StartFusionSession(GameMode mode, string sessionName)
    {
        if (fusionBootstrap == null)
        {
            Debug.LogError("[ModeSelect] Không tìm thấy FusionBootstrap trong Scene!");
            return;
        }

        isConnecting = true;

        // Đảm bảo FusionBootstrap ở trạng thái sẵn sàng trước mỗi lần Host/Join
        ResetFusionBootstrapState();

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeIn()
                                          .Perform();
        }

        // Lưu mã phòng để hiển thị trong Lobby/In-game cho Host chia sẻ với bạn bè
        PlayerPrefs.SetString("CurrentRoomID", sessionName);
        PlayerPrefs.Save();

        // Truyền mã phòng vào FusionBootstrap rồi kích hoạt đúng chế độ
        fusionBootstrap.DefaultRoomName = sessionName;

        Debug.Log($"[ModeSelect] Gọi FusionBootstrap -> Mode: {mode} | Room ID: {sessionName}");

        switch (mode)
        {
            case GameMode.Single:
                fusionBootstrap.StartSinglePlayer();
                break;

            case GameMode.Host:
                fusionBootstrap.StartHost();
                SyncFirebaseRoomOptional(isHost: true, sessionName).Forget();
                break;

            case GameMode.Client:
                fusionBootstrap.StartClient();
                SyncFirebaseRoomOptional(isHost: false, sessionName).Forget();
                break;
        }

        // Đợi 1 frame để FusionBootstrap khởi tạo NetworkRunner rồi gắn theo dõi
        await UniTask.Yield();
        trackedRunner = FindActiveRunner();
        if (trackedRunner != null)
        {
            trackedRunner.RemoveCallbacks(this);
            trackedRunner.AddCallbacks(this);
        }

        MonitorSessionStartAsync(mode, sessionName).Forget();
    }

    private NetworkRunner FindActiveRunner()
    {
        var runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        for (int i = 0; i < runners.Length; i++)
        {
            if (runners[i] != null && runners[i].gameObject.activeInHierarchy && !runners[i].IsShutdown)
                return runners[i];
        }
        return null;
    }

    private async UniTaskVoid MonitorSessionStartAsync(GameMode mode, string roomCode)
    {
        float elapsed = 0f;

        while (isConnecting)
        {
            await UniTask.Yield();
            elapsed += Time.unscaledDeltaTime;

            if (trackedRunner == null)
                trackedRunner = FindActiveRunner();

            // FusionBootstrap không khởi tạo được runner hoặc runner bị tắt do lỗi/không tìm thấy phòng
            if (trackedRunner == null || trackedRunner.IsShutdown)
            {
                string title = mode == GameMode.Client ? "ROOM NOT FOUND" : "HOST FAILED";
                string msg = mode == GameMode.Client
                    ? $"Room \"{roomCode}\" does not exist or has already closed."
                    : "Could not start host session. Check Initial Scene in Prototype Network Start.";

                await HandleSessionFailedAsync(title, msg);
                return;
            }

            // Đã vào phiên thành công (Host/Single mở server xong hoặc Client kết nối vào server xong)
            bool isReady = (mode == GameMode.Client)
                ? (trackedRunner.IsConnectedToServer && trackedRunner.SessionInfo.IsValid)
                : (trackedRunner.IsRunning && trackedRunner.SessionInfo.IsValid);

            if (isReady)
            {
                isConnecting = false;
                return;
            }

            // Quá thời gian chờ mà vẫn chưa vào được
            if (elapsed >= connectTimeoutSeconds)
            {
                string title = mode == GameMode.Client ? "CONNECTION TIMEOUT" : "HOST TIMEOUT";
                string msg = mode == GameMode.Client
                    ? $"Could not join room \"{roomCode}\". Please check the code and try again."
                    : "Timed out while creating room. Please try again.";

                await HandleSessionFailedAsync(title, msg);
                return;
            }
        }
    }

    private async UniTask HandleSessionFailedAsync(string title, string message)
    {
        if (!isConnecting) return;
        isConnecting = false;

        if (trackedRunner != null)
        {
            trackedRunner.RemoveCallbacks(this);
            if (!trackedRunner.IsShutdown)
                await trackedRunner.Shutdown();
            Destroy(trackedRunner.gameObject);
            trackedRunner = null;
        }

        ResetFusionBootstrapState();

        if (SceneController.Instance != null && SceneController.Instance.loadingOverlay != null)
        {
            await SceneController.Instance.loadingOverlay.FadeOutBlack(0.3f);
        }

        ShowErrorPanel(title, message);
    }

    private void ResetFusionBootstrapState()
    {
        if (fusionBootstrap == null) return;

        // Đưa _currentStage của FusionBootstrap về Stage.Disconnected (0) để lần bấm Host/Join tiếp theo hoạt động bình thường
        var stageField = typeof(FusionBootstrap).GetField("_currentStage", BindingFlags.Instance | BindingFlags.NonPublic);
        if (stageField != null && stageField.FieldType.IsEnum)
        {
            stageField.SetValue(fusionBootstrap, System.Enum.ToObject(stageField.FieldType, 0));
        }
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

    private async UniTaskVoid SyncFirebaseRoomOptional(bool isHost, string roomCode)
    {
        if (RoomDatabaseManager.Instance == null || FirebaseManager.RealtimeDB.reference == null) return;
        try
        {
            if (isHost)
            {
                Room room = await RoomDatabaseManager.Instance.CreateRoom(roomCode, 4);
                if (room != null)
                    await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomCode}").SetValueAsync(room.RoomID);
            }
            else
            {
                var snap = await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomCode}").GetValueAsync();
                if (snap.Exists && snap.Value != null)
                {
                    await RoomDatabaseManager.Instance.JoinRoom(snap.Value.ToString());
                    await RoomDatabaseManager.Instance.AddMembers(FirebaseManager.UserID);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ModeSelect] Bỏ qua đồng bộ Firebase (đang test offline): " + e.Message);
        }
    }

    private string GenerateRoomCode(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = ROOM_CHARS[Random.Range(0, ROOM_CHARS.Length)];
        return new string(chars);
    }

    #region INetworkRunnerCallbacks
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (!isConnecting) return;

        string msg = shutdownReason == ShutdownReason.GameNotFound
            ? "Room does not exist. Please check your room code."
            : $"Unable to join room ({shutdownReason}).";

        HandleSessionFailedAsync("JOIN ROOM FAILED", msg).Forget();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (!isConnecting) return;
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}