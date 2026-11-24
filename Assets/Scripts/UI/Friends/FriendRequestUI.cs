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

    private string userID;

    public void UpdateUI(string userID)
    {
        this.userID = userID;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);

        NameTag.text = presence != null ? $"{presence.Name} <color=#D8C89E>#{presence.Tag}</color>" : "Đang tải...";
        Avatar.sprite = avt != null ? avt : defaultAvatar;

        Accept.interactable = true;
        Deny.interactable = true;
        Accept.onClick.AddListener(OnAcceptClick);
        Deny.onClick.AddListener(OnDenyClick);
    }

    private void OnAcceptClick()
    {
        Accept.interactable = false;
        Deny.interactable = false;
        NetworkDataManager.Instance.friendManager.MakeFriend.AcceptFriendRequest(userID);
    }

    private void OnDenyClick()
    {
        Accept.interactable = false;
        Deny.interactable = false;
        NetworkDataManager.Instance.friendManager.MakeFriend.DeclineFriendRequest(userID);
    }

    public void ResetUI()
    {
        Avatar.sprite = defaultAvatar;
        NameTag.text = string.Empty;
        Accept.onClick.RemoveAllListeners();
        Deny.onClick.RemoveAllListeners();
    }
}