using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestUI : MonoBehaviour
{
    [SerializeField] Image Avatar;
    [SerializeField] TextMeshProUGUI NameTag;
    [SerializeField] Button Accept;
    [SerializeField] Button Deny;
    [SerializeField] Sprite defaultAvatar;
    string userID;    
    public void UpdateUI(string userID)
    {
        this.userID = userID;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);
        NameTag.text = $"{presence.Name} #{presence.Tag}";
        Avatar.sprite = avt;
        Accept.onClick.AddListener(OnAcceptClick);
        Deny.onClick.AddListener(OnDenyClick);
    }
    void OnAcceptClick()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.AcceptFriendRequest(userID);
        Destroy(gameObject);
    }
    void OnDenyClick()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.DeclineFriendRequest(userID);
        Destroy(gameObject);
        
    }

    public void ResetUI()
    {
        Avatar.sprite = defaultAvatar;
        NameTag.text = string.Empty;
        Accept.onClick.RemoveAllListeners();
        Deny.onClick.RemoveAllListeners();
    }   
    private void OnDestroy()
    {
        
    }
}
