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
    string userID;
    DatabaseReference @ref;
    public void Init(string userID)
    {
        this.userID = userID;
        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        UpdateUI(presence);
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Presence");
        @ref.ValueChanged += PresenceChanged;
    }

    private async void PresenceChanged(object sender, ValueChangedEventArgs e)
    {
        Presence currentPresence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Presence newPresence = JsonConvert.DeserializeObject<Presence>( e.Snapshot.GetRawJsonValue());
        currentPresence.Name = newPresence.Name;
        currentPresence.Status = newPresence.Status;
        currentPresence.Tag = newPresence.Tag;
        if (currentPresence.AvatarUrl != newPresence.AvatarUrl)
        {
            var avt = await ImgbbUploader.GetAvatar(newPresence.AvatarUrl);
            NetworkDataManager.Instance.SetAvatarUser(userID, avt);
        }
        UpdateUI(newPresence);
    }

    public void OnInviteClick()
    {
        NetworkDataManager.Instance.inviteManager.SendInvite(userID);
    }
    public void OnReqestJoinClick()
    {
        //
    }


    public void UpdateUI(Presence presence)
    {
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);
        if (avt != null)
        {
            avatar.sprite = avt;
        }
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
    public void Refesh()
    {
        UpdateUI(NetworkDataManager.Instance.GetPresenceUser(userID));
    }
    private void OnDestroy()
    {
        @ref.ValueChanged -= PresenceChanged;
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
