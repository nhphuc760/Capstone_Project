using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementFriendUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _name;
    [SerializeField] TextMeshProUGUI status;
    [SerializeField] Image avatar;
    [SerializeField] Button invite;
    [SerializeField] Button requestJoin;
    Presence presence;
    Action onInviteClick;
    Action onRequestJoin;
    private void Start()
    {
        invite.onClick.AddListener(OnInviteClick);
        requestJoin.onClick.AddListener(OnRequestJoinClick);    
        
    }

    public void SetUp(Presence presence, Sprite avatar, Action inviteOnClick, Action requestJoin = null)
    {
        onInviteClick = inviteOnClick;
        onRequestJoin = requestJoin;
        this.presence = presence;
        UpdateUI();
        this.avatar.sprite = avatar;
    }

    public void UpdateUI()
    {
        if (this.presence == null) return;
        _name.text = presence.Name + $" #{presence.Tag}"; 
        status.text = presence.Status.ToString();
    }   

  
    void OnInviteClick()
    {
        onInviteClick?.Invoke();
    }

    void OnRequestJoinClick()
    {
        onRequestJoin?.Invoke();
    }
    private void OnDestroy()
    {
        invite.onClick.RemoveAllListeners();
        requestJoin.onClick.RemoveAllListeners();
    }
}