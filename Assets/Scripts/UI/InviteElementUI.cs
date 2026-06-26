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
    float width;
    Action<bool> selected;

    private void Awake()
    {
       rect = GetComponent<RectTransform>();
        rect.pivot = Vector2.one * .5f;
        rect.anchorMin = new Vector2(1, 0.35f);
        rect.anchorMax = new Vector2(1, 0.35f);
        width = rect.rect.width;
        rect.anchoredPosition = Vector2.zero;
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
        TweenIn();
        Utils.DelayCall(10f, () => TweenOut()).Forget();
    }

    public void OnAccept()
    {
        selected?.Invoke(true);
        selected = null;
        accept.interactable = false;
        TweenOut(); 
    }
    public void OnCancel()
    {
        selected?.Invoke(false);
        selected = null;
        cancel.interactable = false;
        TweenOut();
    }

    void TweenIn()
    {        
        rect.anchoredPosition = new Vector2(width/2, 0);
        rect.DOAnchorPos(new Vector2(-width / 2, 0), 1f)
            .SetEase(Ease.OutBack)
            .OnComplete(
                () =>
                {
                    Destroy(gameObject);
                }
            );
    }
    void TweenOut()
    {
        rect.DOAnchorPos(new Vector2(width / 2, 0), 1f)
            .SetEase(Ease.InBack)
            .OnComplete(
                () => 
                {
                    Destroy(gameObject);
                }
             );
    }

}
