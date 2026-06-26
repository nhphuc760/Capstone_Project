using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InviteElementUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] Image avatar;
    [SerializeField] Image timeBar;
    [SerializeField] Button accept;
    [SerializeField] Button cancel;
    RectTransform rect;
    Action<bool> selected;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    private void OnEnable()
    {
        accept.interactable = true;
        cancel.interactable = true;
    }
   
    public void Show(string title,Invite invite, Action<bool> selected)
    {
        Sprite avt = NetworkDataManager.Instance.avatarsFriend[invite.senderID];
        if(avt != null)
        {
            avatar.sprite = avt;
        }
        this.title.text = title;
        this.selected = selected;
    }

    public void OnAccept()
    {
        selected?.Invoke(true);
        selected = null;
        accept.interactable = false;
    }
    public void OnCancel()
    {
        selected?.Invoke(false);
        selected = null;
        cancel.interactable = false;
    }


}
