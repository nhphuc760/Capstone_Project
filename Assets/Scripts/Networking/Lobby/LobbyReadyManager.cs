using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyReadyManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    public static string PendingDisconnectNotice { get; set; }
    [Header("Tham chiếu UI nút Play / Ready")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("Tự động thoát khi Host rời phòng")]
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private string chooseTypeSceneName = "ChooseType";

    [Header("Tùy chọn luật phòng")]
    [Tooltip("Bật: Single hoặc Host đứng 1 mình được bấm Play ngay. Tắt: Host phải có ít nhất 1 người vào mới được Play.")]
    [SerializeField] private bool allowPlayWhenAlone = true;

    [Header("Màu sắc trạng thái nút")]
    [SerializeField] private Color activePlayColor = new Color32(74, 217, 145, 255);     
    [SerializeField] private Color disabledPlayColor = new Color32(44, 53, 64, 255);     
    [SerializeField] private Color clientNotReadyColor = new Color32(242, 184, 75, 255); 
    [SerializeField] private Color clientReadyColor = new Color32(74, 217, 145, 255);    

    [SerializeField] private Color activeTextColor = new Color32(15, 25, 20, 255);       
    [SerializeField] private Color disabledTextColor = new Color32(130, 145, 160, 255);  

    [Networked]
    private PlayerRef OriginalHost { get; set; }

    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, NetworkBool> ReadyStates => default;

    private Image buttonBackground;
    private string lastVisualSignature = "";
    private bool _startedAsHost;
    private HostDisconnectWatcher _disconnectWatcher;

    private void Awake()
    {
        if (actionButton != null)
        {
            buttonBackground = actionButton.GetComponent<Image>();
            if (actionButtonText == null)
                actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);

            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        if (leaveRoomButton == null)
        {
            GameObject leaveObj = GameObject.Find("Btn_LeaveRoom");
            if (leaveObj != null)
                leaveRoomButton = leaveObj.GetComponent<Button>();
        }
    }

    private void Start()
    {
        RefreshButtonVisuals(forceUpdate: true);
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonClicked);

        // Nếu Client tự bấm Leave chủ động (không phải do Host thoát), dọn Watcher theo LobbyScene
        if (_disconnectWatcher != null && !_disconnectWatcher.IsLeaving)
        {
            Destroy(_disconnectWatcher.gameObject);
        }
    }

    public override void Spawned()
    {
        bool isSingleSession = Runner.GameMode == GameMode.Single || Runner.SessionInfo.MaxPlayers <= 1;
        bool isCurrentMaster = isSingleSession || Runner.IsSharedModeMasterClient || Runner.IsServer;

        if (isCurrentMaster && OriginalHost == PlayerRef.None)
        {
            _startedAsHost = true;
            OriginalHost = Runner.LocalPlayer;
        }
        else if (OriginalHost == Runner.LocalPlayer)
        {
            _startedAsHost = true;
        }

        if (HasStateAuthority)
        {
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                if (!ReadyStates.ContainsKey(player))
                    ReadyStates.Set(player, false);
            }
        }

        if (!_startedAsHost && _disconnectWatcher == null)
        {
            GameObject watcherObj = new GameObject("HostDisconnectWatcher");
            _disconnectWatcher = watcherObj.AddComponent<HostDisconnectWatcher>();
            _disconnectWatcher.Initialize(
                Runner,
                Object,
                OriginalHost,
                actionButton,
                actionButtonText,
                buttonBackground,
                disabledPlayColor,
                disabledTextColor,
                leaveRoomButton,
                chooseTypeSceneName
            );
        }

        RefreshButtonVisuals(forceUpdate: true);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!_startedAsHost && _disconnectWatcher != null)
        {
            _disconnectWatcher.ForceTriggerLeave("LobbyManager bị despawn do Host rời phòng.");
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            ReadyStates.Set(player, false);
        }

        Debug.Log($"[Lobby] Người chơi {player} vừa vào phòng. Cập nhật lại trạng thái Ready.");
        RefreshButtonVisuals(forceUpdate: true);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority && ReadyStates.ContainsKey(player))
        {
            ReadyStates.Remove(player);
        }

        if (!_startedAsHost && OriginalHost != PlayerRef.None && player == OriginalHost && _disconnectWatcher != null)
        {
            _disconnectWatcher.ForceTriggerLeave("Host đã rời khỏi phòng.");
            return;
        }

        Debug.Log($"[Lobby] Người chơi {player} đã rời phòng. Cập nhật lại điều kiện Play của Host.");
        RefreshButtonVisuals(forceUpdate: true);
    }

    public override void Render()
    {
        if (_disconnectWatcher != null && _disconnectWatcher.IsLeaving) return;
        RefreshButtonVisuals(forceUpdate: false);
    }

    private void OnActionButtonClicked()
    {
        if (_disconnectWatcher != null && _disconnectWatcher.IsLeaving) return;

        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid)
        {
            Debug.Log("[Lobby - Single] Đã bấm PLAY! Đủ điều kiện vào game (Sẽ load GameplayScene sau).");
            return;
        }

        if (_startedAsHost)
        {
            if (CanHostStartGame(out int totalPlayers, out int readyClients, out int totalClients))
            {
                if (totalClients == 0)
                    Debug.Log("[Lobby - Host/Single] Phòng đang có 1 mình -> Đủ điều kiện bấm PLAY vào game!");
                else
                    Debug.Log($"[Lobby - Host] Tất cả thành viên ({readyClients}/{totalClients}) đã READY! Đủ điều kiện bấm PLAY vào game (Tổng {totalPlayers} người).");
            }
            else
            {
                Debug.LogWarning($"[Lobby - Host] Chưa thể bắt đầu! Mới có {readyClients}/{totalClients} thành viên Ready.");
            }
            return;
        }

        bool currentReady = IsPlayerReady(Runner.LocalPlayer);
        bool nextReady = !currentReady;

        Debug.Log($"[Lobby - Client] Người chơi {Runner.LocalPlayer} đổi trạng thái sang: {(nextReady ? "READY" : "CHƯA READY")}");
        RPC_SetPlayerReady(Runner.LocalPlayer, nextReady);
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerReady(PlayerRef player, NetworkBool isReady)
    {
        if (Object == null || !Object.IsValid) return;
        ReadyStates.Set(player, isReady);
    }

    private bool IsPlayerReady(PlayerRef player)
    {
        if (Object == null || !Object.IsValid) return false;
        return ReadyStates.TryGet(player, out NetworkBool ready) && ready;
    }

    private bool CanHostStartGame(out int totalPlayers, out int readyClients, out int totalClients)
    {
        totalPlayers = 1;
        readyClients = 0;
        totalClients = 0;

        if (Runner == null || !Runner.IsRunning)
            return allowPlayWhenAlone;

        var activePlayers = Runner.ActivePlayers.ToList();
        totalPlayers = activePlayers.Count;

        foreach (PlayerRef p in activePlayers)
        {
            if (p == Runner.LocalPlayer) continue;
            totalClients++;
            if (IsPlayerReady(p))
                readyClients++;
        }

        if (totalClients == 0)
            return allowPlayWhenAlone;

        return readyClients == totalClients;
    }

    private void RefreshButtonVisuals(bool forceUpdate)
    {
        if (actionButton == null) return;

        string label;
        Color bgColor;
        Color txtColor;
        bool canClick;

        if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid)
        {
            label = "PLAY";
            bgColor = activePlayColor;
            txtColor = activeTextColor;
            canClick = true;
        }
        else if (_startedAsHost)
        {
            bool canStart = CanHostStartGame(out _, out int readyClients, out int totalClients);
            label = totalClients == 0 ? "PLAY" : $"PLAY ({readyClients}/{totalClients})";
            bgColor = canStart ? activePlayColor : disabledPlayColor;
            txtColor = canStart ? activeTextColor : disabledTextColor;
            canClick = canStart;
        }
        else
        {
            bool isLocalReady = IsPlayerReady(Runner.LocalPlayer);
            label = isLocalReady ? "READY ✓" : "READY";
            bgColor = isLocalReady ? clientReadyColor : clientNotReadyColor;
            txtColor = activeTextColor;
            canClick = true;
        }

        string signature = $"{label}|{canClick}|{ColorUtility.ToHtmlStringRGBA(bgColor)}";
        if (!forceUpdate && signature == lastVisualSignature) return;
        lastVisualSignature = signature;

        actionButton.interactable = canClick;

        if (actionButtonText != null)
        {
            actionButtonText.text = label;
            actionButtonText.color = txtColor;
        }

        if (buttonBackground != null)
            buttonBackground.color = bgColor;
    }

    private sealed class HostDisconnectWatcher : MonoBehaviour
    {
        private NetworkRunner _runner;
        private NetworkObject _lobbyNetworkObject;
        private PlayerRef _originalHost;
        private Button _actionButton;
        private TextMeshProUGUI _actionButtonText;
        private Image _buttonBackground;
        private Color _disabledBgColor;
        private Color _disabledTextColor;
        private Button _leaveButton;
        private string _targetSceneName;

        public bool IsLeaving { get; private set; }

        public void Initialize(
            NetworkRunner runner,
            NetworkObject lobbyNetworkObject,
            PlayerRef originalHost,
            Button actionButton,
            TextMeshProUGUI actionButtonText,
            Image buttonBackground,
            Color disabledBgColor,
            Color disabledTextColor,
            Button leaveButton,
            string targetSceneName)
        {
            _runner = runner;
            _lobbyNetworkObject = lobbyNetworkObject;
            _originalHost = originalHost;
            _actionButton = actionButton;
            _actionButtonText = actionButtonText;
            _buttonBackground = buttonBackground;
            _disabledBgColor = disabledBgColor;
            _disabledTextColor = disabledTextColor;
            _leaveButton = leaveButton;
            _targetSceneName = targetSceneName;
        }

        private void Update()
        {
            if (IsLeaving) return;

            bool runnerDisconnected = _runner == null || !_runner.IsRunning;
            bool lobbyDestroyed = _lobbyNetworkObject == null || !_lobbyNetworkObject.IsValid;
            bool hostLeft = !runnerDisconnected && _originalHost != PlayerRef.None && !_runner.ActivePlayers.Contains(_originalHost);

            if (runnerDisconnected || lobbyDestroyed || hostLeft)
            {
                ForceTriggerLeave("Phát hiện Host đã ngắt kết nối hoặc rời phòng.");
            }
        }

        public void ForceTriggerLeave(string reason)
        {
            if (IsLeaving) return;
            IsLeaving = true;
            LobbyReadyManager.PendingDisconnectNotice = "Host has left the room";
            // Giữ Watcher sống qua lúc chuyển cảnh để dọn dẹp EventSystem & LoadingOverlay ở ChooseType
            DontDestroyOnLoad(gameObject);

            Debug.LogWarning($"[Lobby] {reason} Đang đưa người chơi về {_targetSceneName}...");

            if (_actionButton != null)
                _actionButton.interactable = false;
            if (_actionButtonText != null)
            {
                _actionButtonText.text = "HOST LEFT...";
                _actionButtonText.color = _disabledTextColor;
            }
            if (_buttonBackground != null)
                _buttonBackground.color = _disabledBgColor;

            ExecuteLeaveAsync().Forget();
        }

        private async UniTaskVoid ExecuteLeaveAsync()
        {
            try
            {
                // 1. Thử dùng luồng thoát phòng có sẵn của Btn_LeaveRoom
                if (_leaveButton != null)
                {
                    try
                    {
                        _leaveButton.onClick.Invoke();
                        _leaveButton.interactable = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Lobby] Không gọi được nút Leave mặc định: {ex.Message}");
                    }
                }

                // 2. Chờ tối đa 1.2s xem luồng của Btn_LeaveRoom đã tự chuyển sang ChooseType chưa
                float elapsed = 0f;
                while (elapsed < 1.2f && SceneManager.GetActiveScene().name != _targetSceneName)
                {
                    await UniTask.Yield();
                    elapsed += Time.unscaledDeltaTime;
                }

                // 3. Nếu chưa chuyển được (do Runner đã tắt từ trước), chủ động dọn và tải thẳng ChooseType
                if (SceneManager.GetActiveScene().name != _targetSceneName)
                {
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

                    if (_runner != null)
                    {
                        try
                        {
                            await _runner.Shutdown();
                        }
                        catch { /* Runner đã tắt */ }
                    }

                    await SceneManager.LoadSceneAsync(_targetSceneName, LoadSceneMode.Single);
                }

                // 4. Đợi 1 frame sau khi ChooseType kích hoạt rồi mở khóa hoàn toàn input & UI
                await UniTask.Yield();
                RestoreChooseTypeInteractivity();

                if (SceneController.Instance != null)
                {
                    await SceneController.Instance.NewTransitionPlan()
                        .WithFadeOut()
                        .Perform();

                    UnblockSceneControllerOverlayRaycasts();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lobby] Lỗi khi khôi phục tương tác ở {_targetSceneName}: {ex.Message}");
            }
            finally
            {
                Destroy(gameObject);
            }
        }

        private void RestoreChooseTypeInteractivity()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Giữ duy nhất 1 EventSystem (ưu tiên thuộc SceneController) để tránh kẹt chuột
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem primaryEs = SceneController.Instance != null
                ? SceneController.Instance.GetComponentInChildren<EventSystem>(true)
                : null;

            if (primaryEs == null && eventSystems.Length > 0)
                primaryEs = eventSystems[0];

            foreach (EventSystem es in eventSystems)
            {
                if (es == null || es == primaryEs) continue;
                es.enabled = false;
                Destroy(es.gameObject);
            }

            if (primaryEs != null)
            {
                primaryEs.gameObject.SetActive(true);
                primaryEs.enabled = false;
                primaryEs.enabled = true;
            }

            // Giữ duy nhất 1 AudioListener
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioListener primaryListener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
            if (primaryListener == null && listeners.Length > 0)
                primaryListener = listeners[0];

            foreach (AudioListener listener in listeners)
            {
                if (listener == null || listener == primaryListener) continue;
                listener.enabled = false;
                Destroy(listener);
            }

            if (primaryListener != null)
                primaryListener.enabled = true;

            UnblockSceneControllerOverlayRaycasts();
        }

        private void UnblockSceneControllerOverlayRaycasts()
        {
            if (SceneController.Instance == null) return;

            foreach (CanvasGroup cg in SceneController.Instance.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (cg.alpha <= 0.05f)
                    cg.blocksRaycasts = false;
            }

            foreach (Image img in SceneController.Instance.GetComponentsInChildren<Image>(true))
            {
                if (img.color.a <= 0.05f)
                    img.raycastTarget = false;
            }
        }
    }
}