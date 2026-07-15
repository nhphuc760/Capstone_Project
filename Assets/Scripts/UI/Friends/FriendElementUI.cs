using System;
using Cysharp.Threading.Tasks.Triggers;
using Firebase.Database;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendElementUI : MonoBehaviour
{
    [SerializeField] Image avatar;
    [SerializeField] TextMeshProUGUI nameTag;
    [SerializeField] TextMeshProUGUI status;
    [SerializeField] Button invite;
    [SerializeField] Button requestJoin;
    [SerializeField] Sprite defaultAvatar;
    string userID;
    public void Init(string userID)
    {
        this.userID = userID;
        UpdateUI(); 
    }

    public void OnInviteClick()
    {
        NetworkDataManager.Instance.inviteManager.SendInvite(userID);
    }
    public void OnReqestJoinClick()
    {
        //
    }   

    public void UpdateUI()
    {
        if(string.IsNullOrEmpty(userID)) return;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);
        avatar.sprite = avt != null ? avt : defaultAvatar;     
        nameTag.text = $"{presence.Name} #{presence.Tag}";
        status.text = GetStatusText(presence.Status);
        if (presence.Status != OnlineStatus.Offline)
        {
            transform.SetAsFirstSibling();
            switch (presence.Status) 
            {
                case OnlineStatus.Online:
                    invite.gameObject.SetActive(true);
                    requestJoin.gameObject.SetActive(false);
                    break;
                case OnlineStatus.InMatch:
                    invite.gameObject.SetActive(false);
                    requestJoin.gameObject.SetActive(false);
                    break;
                case OnlineStatus.InParty:
                    invite.gameObject.SetActive(false);
                    requestJoin.gameObject.SetActive(true);
                    break;
            }
        }
        else
        {            
            invite.gameObject.SetActive(false); 
            requestJoin.gameObject.SetActive(false);
        }
    }     
    private string GetStatusText(OnlineStatus status)
    {
        return status switch
        {
            OnlineStatus.Online =>
                "Online".ToColor(Color.green),

            OnlineStatus.Offline =>
                "Offline".ToColor(Color.gray),

            OnlineStatus.InParty =>
                "InParty".ToColor(Color.white),

            OnlineStatus.InMatch =>
                "InMatch".ToColor(Color.blue),

            _ => status.ToString()
        };
    }

}
