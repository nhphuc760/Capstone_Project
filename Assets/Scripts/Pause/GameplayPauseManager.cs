using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayPauseManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameplayPauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pauseRootPanel;
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnLeaveMatch;
    [SerializeField] private TextMeshProUGUI txtLeaveButtonLabel;

    [Header("Scene")]
    [SerializeField] private string chooseTypeSceneName = "ChooseType";

    private NetworkRunner _runner;
    private bool _isSinglePlay;
    private bool _isHost;
    private PlayerRef _hostPlayerRef = PlayerRef.None;
    private bool _isLeavingMatch;
    private bool _isMatchEnded;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsPaused = false;
        _isMatchEnded = false;

        if (pauseRootPanel != null) pauseRootPanel.SetActive(false);

        if (btnResume != null) btnResume.onClick.AddListener(ResumeGame);
        if (btnLeaveMatch != null) btnLeaveMatch.onClick.AddListener(() => LeaveMatchAsync().Forget());
    }

    private void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null && _runner.IsRunning)
        {
            _runner.RemoveCallbacks(this);
            _runner.AddCallbacks(this);

            _isSinglePlay = _runner.GameMode == GameMode.Single
                            || (_runner.SessionInfo.IsValid && _runner.SessionInfo.MaxPlayers == 1);
            _isHost = _isSinglePlay || _runner.IsServer || _runner.IsSharedModeMasterClient;

            if (!_isSinglePlay)
            {
                _hostPlayerRef = _isHost
                    ? _runner.LocalPlayer
                    : _runner.ActivePlayers.OrderBy(p => p.PlayerId).FirstOrDefault();
            }
        }
        else
        {
            _isSinglePlay = true;
            _isHost = true;
        }

        if (txtLeaveButtonLabel != null)
        {
            txtLeaveButtonLabel.text = (_isHost && !_isSinglePlay) ? "END MATCH" : "LEAVE MATCH";
        }
    }

    private void OnDestroy()
    {
        IsPaused = false;
        if (Instance == this) Instance = null;

        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }

    private void Update()
    {
        if (_isLeavingMatch) return;

        // Dự phòng kiểm tra nếu Host đột ngột rớt mạng trong chế độ Multiplayer
        if (!_isSinglePlay && !_isHost)
        {
            bool runnerStopped = _runner == null || !_runner.IsRunning;
            bool hostMissing = !runnerStopped && _hostPlayerRef != PlayerRef.None && !_runner.ActivePlayers.Contains(_hostPlayerRef);
            if (runnerStopped || hostMissing)
            {
                TriggerHostLeftReturn();
                return;
            }
        }

        if (_isMatchEnded) return;

        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (!escPressed) return;

        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (_isLeavingMatch || _isMatchEnded || IsPaused) return;
        IsPaused = true;

        _previousLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkPlayer.Local != null && NetworkPlayer.Local.Object != null && NetworkPlayer.Local.Object.HasStateAuthority)
            NetworkPlayer.Local.Lock = true;

        if (pauseRootPanel != null) pauseRootPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (pauseRootPanel != null) pauseRootPanel.SetActive(false);

        if (NetworkPlayer.Local != null && NetworkPlayer.Local.Object != null && NetworkPlayer.Local.Object.HasStateAuthority)
            NetworkPlayer.Local.Lock = false;

        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;
    }

    /// <summary>
    /// Gán trực tiếp vào sự kiện OnClick() của nút thoát trên cả 2 UI GameWin và GameOver.
    /// </summary>
    public void ExitToChooseType()
    {
        LeaveMatchAsync().Forget();
    }

    /// <summary>
    /// Gọi khi trận đấu kết thúc (lúc bật bảng GameWin hoặc GameOver) để mở khóa chuột và chặn phím Esc.
    /// </summary>
    public void OnMatchEnded()
    {
        _isMatchEnded = true;
        if (IsPaused)
            ResumeGame();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void TriggerHostLeftReturn()
    {
        if (_isLeavingMatch) return;
        if (!_isMatchEnded)
            LobbyReadyManager.PendingDisconnectNotice = "Host has left the room";

        LeaveMatchAsync().Forget();
    }

    private async UniTaskVoid LeaveMatchAsync()
    {
        if (_isLeavingMatch) return;
        _isLeavingMatch = true;
        IsPaused = false;

        if (btnResume != null) btnResume.interactable = false;
        if (btnLeaveMatch != null) btnLeaveMatch.interactable = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        try
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
                _runner.RemoveCallbacks(this);
                if (_runner.IsRunning)
                {
                    try
                    {
                        await _runner.Shutdown();
                    }
                    catch { /* Runner đã đóng */ }
                }
                Destroy(_runner.gameObject);
                _runner = null;
            }

            await SceneManager.LoadSceneAsync(chooseTypeSceneName, LoadSceneMode.Single);
            await UniTask.Yield();

            RestoreChooseTypeSceneState();

            if (SceneController.Instance != null)
            {
                await SceneController.Instance.NewTransitionPlan()
                    .WithFadeOut()
                    .Perform();

                UnblockOverlayRaycasts();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameplayPause] Lỗi khi rời trận về {chooseTypeSceneName}: {ex.Message}");
        }
    }

    private void RestoreChooseTypeSceneState()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        UnblockOverlayRaycasts();
    }

    private void UnblockOverlayRaycasts()
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

    #region INetworkRunnerCallbacks
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!_isSinglePlay && !_isHost && _hostPlayerRef != PlayerRef.None && player == _hostPlayerRef)
        {
            TriggerHostLeftReturn();
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (!_isSinglePlay && !_isHost)
        {
            TriggerHostLeftReturn();
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (!_isSinglePlay && !_isHost && !_isLeavingMatch)
        {
            TriggerHostLeftReturn();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    #endregion
}