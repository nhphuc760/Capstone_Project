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
    [SerializeField] Image pendingRequest;
    const string RESAULTSEARCH_KEY = "RESULTSEARCHUI";
    string userID;
    

    public void UpdateUI(string userID)
    {
        if (string.IsNullOrEmpty(userID)) return;
        this.userID = userID;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avatar = NetworkDataManager.Instance.GetAvatarUser(userID);
        Avatar.sprite = avatar;
        NameTag.text = $"{presence.Tag} #{presence.Tag}";
        status.text = GetStatusText(presence.Status);
        //nếu người này đã được gửi lời mời trước đó
        if (NetworkDataManager.Instance.friendManager.MakeFriend.HasPending(userID))
        {
            RequestFriend.gameObject.SetActive(false);
            Cancel.gameObject.SetActive(true);
            pendingRequest.gameObject.SetActive(true);
            Cancel.onClick.AddListener(OnCancelPendingRequest);
        }
        else // Nếu chưa
        {
            Cancel.gameObject.SetActive(false);
            pendingRequest.gameObject.SetActive(false);
            RequestFriend.gameObject.SetActive(true);
            RequestFriend.onClick.AddListener(OnRequestFriendClick);
        }

    }

    public void ResetUI()
    {
        NameTag.text = string.Empty;
        status.text = string.Empty;
        Avatar.sprite = defaultAvatar;
        RequestFriend.onClick.RemoveAllListeners();
        Cancel.onClick.RemoveAllListeners();
    }

    void OnCancelPendingRequest()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.CancelFriendRequest(userID);
        ObjectPoolManager.Ins.Release(RESAULTSEARCH_KEY, gameObject);
    }

    void OnRequestFriendClick()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.SendFriendRequest(userID);
    }
    private void OnDisable()
    {
        ResetUI();
    }

    string GetStatusText(OnlineStatus status)
    {
        switch (status)
        {
            case OnlineStatus.Online:
                return status.ToString().ToColor(Color.green);
            case OnlineStatus.Offline:
                return status.ToString().ToColor(Color.gray);
            case OnlineStatus.InParty:
                return status.ToString().ToColor(Color.white);
            case OnlineStatus.InMatch:
                return status.ToString().ToColor(Color.blue);
            default:
                return status.ToString();
        }
    }
}
