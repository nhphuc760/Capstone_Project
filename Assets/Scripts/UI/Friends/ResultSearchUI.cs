using Newtonsoft.Json;
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
    const string RESAULTSEARCH_KEY = "RESULTSEARCHUI";
    string userID;

    private void OnEnable()
    {
        Cancel.onClick.AddListener(OnCancelPendingRequest);
        RequestFriend.onClick.AddListener(OnRequestFriendClick);
    }

    public void UpdateUI(string userID)
    {
        if (string.IsNullOrEmpty(userID)) return;
        this.userID = userID;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Debug.Log(JsonConvert.SerializeObject(presence));
        Sprite avatar = NetworkDataManager.Instance.GetAvatarUser(userID);
        Avatar.sprite = avatar;
        NameTag.text = $"{presence.Name} #{presence.Tag}";
        status.text = GetStatusText(presence.Status);        
        //nếu người này đã được gửi lời mời trước đó
        ShowRequestButtonOrNot(!NetworkDataManager.Instance.friendManager.MakeFriend.HasPending(userID));
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
        ShowRequestButtonOrNot(true);
    }

    void OnRequestFriendClick()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.SendFriendRequest(userID);
        ShowRequestButtonOrNot(false); 
    }

    void ShowRequestButtonOrNot(bool value)
    {
        if (value)
        {
            RequestFriend.gameObject.SetActive(true);
            Cancel.gameObject.SetActive(false);
            pendingRequestIcon.gameObject.SetActive(false);
        }
        else
        {
            RequestFriend.gameObject.SetActive(false);
            Cancel.gameObject.SetActive(true);
            pendingRequestIcon.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        ResetUI();
        if (gameObject.activeSelf)
        {
            ObjectPoolManager.Ins.Release(RESAULTSEARCH_KEY, gameObject);
        }
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
