using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultSearchUI : MonoBehaviour
{
    [SerializeField] Image Avatar;
    [SerializeField] TextMeshProUGUI NameTag;
    [SerializeField] TextMeshProUGUI status;
    [SerializeField] Button RequestFriend;
    [SerializeField] Sprite defaultAvatar;
    [SerializeField] Button Cancel;
    [SerializeField] Image pendingRequestIcon;

    private string userID;

    private void OnEnable()
    {
        Cancel.onClick.RemoveAllListeners();
        RequestFriend.onClick.RemoveAllListeners();
        Cancel.onClick.AddListener(OnCancelPendingRequest);
        RequestFriend.onClick.AddListener(OnRequestFriendClick);

        if (NetworkDataManager.Instance?.friendManager?.MakeFriend != null)
            NetworkDataManager.Instance.friendManager.MakeFriend.onPendingChanged += OnPendingChanged;
    }

    private void OnDisable()
    {
        if (NetworkDataManager.Instance?.friendManager?.MakeFriend != null)
            NetworkDataManager.Instance.friendManager.MakeFriend.onPendingChanged -= OnPendingChanged;

        ResetUI();
    }

    private void OnPendingChanged(string changedUserId)
    {
        if (changedUserId == userID)
            RefreshActionButtons();
    }

    public void UpdateUI(string userID)
    {
        if (string.IsNullOrEmpty(userID)) return;
        this.userID = userID;

        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avatar = NetworkDataManager.Instance.GetAvatarUser(userID);

        Avatar.sprite = avatar != null ? avatar : defaultAvatar;

        if (presence != null)
        {
            NameTag.text = $"{presence.Name} <color=#9AA6B2>#{presence.Tag}</color>";
            status.text = GetStatusText(presence.Status);
        }
        else
        {
            NameTag.text = "Unknown";
            status.text = "Offline".ToColor(Color.gray);
        }

        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        if (string.IsNullOrEmpty(userID)) return;

        var makeFriend = NetworkDataManager.Instance.friendManager.MakeFriend;
        if (NetworkDataManager.Instance.friendManager.IsFriend(userID))
        {
            RequestFriend.gameObject.SetActive(false);
            Cancel.gameObject.SetActive(false);
            if (pendingRequestIcon != null) pendingRequestIcon.gameObject.SetActive(false);
            return;
        }

        ShowRequestButtonOrNot(!makeFriend.HasPending(userID));
    }

    public void ResetUI()
    {
        userID = null;
        NameTag.text = string.Empty;
        status.text = string.Empty;
        Avatar.sprite = defaultAvatar;
        RequestFriend.onClick.RemoveAllListeners();
        Cancel.onClick.RemoveAllListeners();
    }

    private void OnCancelPendingRequest()
    {
        if (string.IsNullOrEmpty(userID)) return;
        NetworkDataManager.Instance.friendManager.MakeFriend.CancelFriendRequest(userID);
        ShowRequestButtonOrNot(true);
    }

    private void OnRequestFriendClick()
    {
        if (string.IsNullOrEmpty(userID)) return;
        NetworkDataManager.Instance.friendManager.MakeFriend.SendFriendRequest(userID);
        RefreshActionButtons();
    }

    private void ShowRequestButtonOrNot(bool showAddButton)
    {
        RequestFriend.gameObject.SetActive(showAddButton);
        Cancel.gameObject.SetActive(!showAddButton);
        if (pendingRequestIcon != null)
            pendingRequestIcon.gameObject.SetActive(!showAddButton);
    }

    private string GetStatusText(OnlineStatus onlineStatus)
    {
        return onlineStatus switch
        {
            OnlineStatus.Online => onlineStatus.ToString().ToColor(Color.green),
            OnlineStatus.Offline => onlineStatus.ToString().ToColor(Color.gray),
            OnlineStatus.InParty => "In Lobby".ToColor(Color.yellow),
            OnlineStatus.InMatch => onlineStatus.ToString().ToColor(Color.cyan),
            _ => onlineStatus.ToString()
        };
    }
}