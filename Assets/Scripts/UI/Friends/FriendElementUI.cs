using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendElementUI : MonoBehaviour
{
    [SerializeField] private Image avatar;
    [SerializeField] private Image statusDot;
    [SerializeField] private TextMeshProUGUI nameTag;
    [SerializeField] private TextMeshProUGUI status;
    [SerializeField] private Button invite;
    [SerializeField] private TextMeshProUGUI inviteButtonText;
    [SerializeField] private Button requestJoin;
    [SerializeField] private Button unfriendButton;
    [SerializeField] private Sprite defaultAvatar;

    private string userID;
    private bool isInviting;
    private bool isRequesting;
    private bool _lastInLobbyState;
    private float _lobbyCheckTimer;

    private void Awake()
    {
        SubscribeButtons();
    }

    private void OnEnable()
    {
        RoomDatabaseManager.OnRoomStateChanged += HandleRoomStateChanged;
        if (!string.IsNullOrEmpty(userID))
            UpdateUI();
    }

    private void OnDisable()
    {
        RoomDatabaseManager.OnRoomStateChanged -= HandleRoomStateChanged;
    }

    private void HandleRoomStateChanged(Room room)
    {
        if (!string.IsNullOrEmpty(userID))
            UpdateUI();
    }

    private void OnDestroy()
    {
        UnsubscribeButtons();
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(userID)) return;

        _lobbyCheckTimer += Time.unscaledDeltaTime;
        if (_lobbyCheckTimer < 0.4f) return;
        _lobbyCheckTimer = 0f;

        bool currentInLobby = IsLocalPlayerInLobby();
        if (currentInLobby != _lastInLobbyState)
        {
            _lastInLobbyState = currentInLobby;
            UpdateUI();
        }
    }

    private bool IsLocalPlayerInLobby()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LobbyScene")
            return false;

        var activeRunner = UnityEngine.Object.FindAnyObjectByType<Fusion.NetworkRunner>();
        if (LobbyUIManager.IsSinglePlaySession(activeRunner))
            return false;

        return true;
    }

    public void Init(string userID)
    {
        this.userID = userID;
        this.isInviting = false;

        RoomDatabaseManager.OnRoomStateChanged -= HandleRoomStateChanged;
        RoomDatabaseManager.OnRoomStateChanged += HandleRoomStateChanged;

        if (invite != null) invite.interactable = true;
        if (requestJoin != null) requestJoin.interactable = true;

        UpdateUI();
    }

    public async void OnInviteClick()
    {
        if (isInviting || string.IsNullOrEmpty(userID)) return;

        var activeRunner = UnityEngine.Object.FindAnyObjectByType<Fusion.NetworkRunner>();
        if (LobbyUIManager.IsSinglePlaySession(activeRunner) || !IsLocalPlayerInLobby())
        {
            Debug.LogWarning("[FriendUI] Chế độ Single hoặc chưa vào phòng Multiplayer, không thể mời bạn bè!");
            return;
        }

        if (NetworkDataManager.Instance?.inviteManager == null) return;

        isInviting = true;
        if (invite != null) invite.interactable = false;
        if (inviteButtonText != null) inviteButtonText.text = "INVITED ✓";

        try
        {
            NetworkDataManager.Instance.inviteManager.SendInvite(userID);
            await UniTask.Delay(TimeSpan.FromSeconds(2.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            isInviting = false;
            if (invite != null) invite.interactable = true;
            if (inviteButtonText != null) inviteButtonText.text = "INVITE";
        }
    }

    public async void OnRequestJoinClick()
    {
        if (isRequesting || string.IsNullOrEmpty(userID)) return;
        if (NetworkDataManager.Instance?.inviteManager == null) return;

        isRequesting = true;
        if (requestJoin != null) requestJoin.interactable = false;

        try
        {
            bool sent = await NetworkDataManager.Instance.inviteManager.SendJoinRequestAsync(userID);
            if (sent)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            isRequesting = false;
            if (requestJoin != null) requestJoin.interactable = true;
        }
    }

    private void OnUnfriendClick()
    {
        if (string.IsNullOrEmpty(userID)) return;
        if (unfriendButton != null) unfriendButton.interactable = false;
        NetworkDataManager.Instance?.friendManager?.UnfriendBidirectionalAsync(userID).Forget();
    }

    public void UpdateUI()
    {
        if (string.IsNullOrEmpty(userID)) return;

        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);

        if (avatar != null)
            avatar.sprite = avt != null ? avt : defaultAvatar;

        if (presence == null)
        {
            if (nameTag != null) nameTag.text = "Đang tải...";
            if (status != null) status.text = "Offline".ToColor(Color.gray);
            if (statusDot != null) statusDot.color = Color.gray;
            if (invite != null) invite.gameObject.SetActive(false);
            if (requestJoin != null) requestJoin.gameObject.SetActive(false);
            return;
        }

        if (nameTag != null) nameTag.text = $"{presence.Name} <color=#9AA6B2>#{presence.Tag}</color>";
        if (status != null) status.text = GetStatusText(presence.Status);
        if (statusDot != null) statusDot.color = GetStatusColor(presence.Status);

        TriggerButtonWithOnlineStatus(presence.Status);
    }

    private Color GetStatusColor(OnlineStatus onlineStatus)
    {
        return onlineStatus switch
        {
            OnlineStatus.Online => new Color32(74, 217, 145, 255),
            OnlineStatus.InParty => new Color32(242, 184, 75, 255),
            OnlineStatus.InMatch => new Color32(86, 180, 233, 255),
            _ => new Color32(120, 130, 140, 255)
        };
    }

    private string GetStatusText(OnlineStatus onlineStatus)
    {
        return onlineStatus switch
        {
            OnlineStatus.Online => "Online".ToColor(Color.green),
            OnlineStatus.Offline => "Offline".ToColor(Color.gray),
            OnlineStatus.InParty => "In Lobby".ToColor(Color.yellow),
            OnlineStatus.InMatch => "In Match".ToColor(Color.cyan),
            _ => onlineStatus.ToString()
        };
    }

    private void SubscribeButtons()
    {
        UnsubscribeButtons();
        if (invite != null) invite.onClick.AddListener(OnInviteClick);
        if (requestJoin != null) requestJoin.onClick.AddListener(OnRequestJoinClick);
        if (unfriendButton != null) unfriendButton.onClick.AddListener(OnUnfriendClick);
    }

    private void UnsubscribeButtons()
    {
        if (invite != null) invite.onClick.RemoveListener(OnInviteClick);
        if (requestJoin != null) requestJoin.onClick.RemoveListener(OnRequestJoinClick);
        if (unfriendButton != null) unfriendButton.onClick.RemoveListener(OnUnfriendClick);
    }

    private void TriggerButtonWithOnlineStatus(OnlineStatus onlineStatus)
    {
        bool isLocalInLobby = IsLocalPlayerInLobby();
        _lastInLobbyState = isLocalInLobby;

        if (onlineStatus != OnlineStatus.Offline)
        {
            transform.SetAsFirstSibling();

            switch (onlineStatus)
            {
                case OnlineStatus.Online:
                    if (invite != null) invite.gameObject.SetActive(isLocalInLobby);
                    if (inviteButtonText != null && !isInviting) inviteButtonText.text = "INVITE";
                    if (requestJoin != null) requestJoin.gameObject.SetActive(false);
                    break;

                case OnlineStatus.InMatch:
                    if (invite != null) invite.gameObject.SetActive(false);
                    if (requestJoin != null) requestJoin.gameObject.SetActive(false);
                    break;

                case OnlineStatus.InParty:
                    bool canRequestJoin = !isLocalInLobby;
                    if (invite != null) invite.gameObject.SetActive(false);
                    if (requestJoin != null) requestJoin.gameObject.SetActive(canRequestJoin);
                    break;
            }
        }
        else
        {
            transform.SetAsLastSibling();
            if (invite != null) invite.gameObject.SetActive(false);
            if (requestJoin != null) requestJoin.gameObject.SetActive(false);
        }
    }
}