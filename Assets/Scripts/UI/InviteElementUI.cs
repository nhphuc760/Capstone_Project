using System;
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
    UniTaskCompletionSource<bool> tcs;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rect.pivot = Vector2.one * .5f;
        rect.anchorMin = new Vector2(1, 0.35f);
        rect.anchorMax = new Vector2(1, 0.35f);
        width = rect.rect.width;
        rect.anchoredPosition = Vector2.zero;
        accept.interactable = true;
        cancel.interactable = true;
    }   

    public UniTask<bool> Show(string title,Sprite avt)
    {        
        tcs = new UniTaskCompletionSource<bool>();
        avatar.sprite = avt;
        this.title.text = title;        
        TweenIn();
        Utils.DelayCall(10f, () => TweenOut()).Forget();
        return tcs.Task;
    }

    public void OnAccept()
    {
        tcs?.TrySetResult(true);
        tcs = null;
        accept.interactable = false;
        TweenOut(); 
    }
    public void OnCancel()
    {
        tcs?.TrySetResult(false);
        tcs = null;
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
                    if(this != null)
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
                    if(this != null)
                        Destroy(gameObject);
                }
             );
    }

}
