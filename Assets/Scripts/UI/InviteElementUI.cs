using System;
using System.Threading;
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
    CancellationTokenSource cts;
    const string INVITEELEMENT_KEY = "INVITEELEMENT";
    private void Awake()
    {
        SubcribeButton();
    }

    private void OnEnable()
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
        UpdateInvite();
        return tcs.Task;
    }    

     void OnAccept()
    {
        tcs?.TrySetResult(true);
        tcs = null;
        accept.interactable = false;        
        TweenOut(); 
    }
    void OnCancel()
    {
        tcs?.TrySetResult(false);
        tcs = null;
        cancel.interactable = false;
        TweenOut();
    }

    public void UpdateInvite()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        TimebarRoutine(cts.Token).Forget();
        TweenIn();
    }

    void TweenIn()
    {        
        rect.anchoredPosition = new Vector2(width/2, 0);
        rect.DOAnchorPos(new Vector2(-width / 2, 0), 1f)
            .SetEase(Ease.OutBack);
    }
    void TweenOut()
    {
        cts?.Cancel();
        cts?.Dispose();
       var operation = rect.DOAnchorPos(new Vector2(width / 2, 0), 1f)
            .SetEase(Ease.InBack)
            .OnComplete(
                () => 
                {
                    if (gameObject.activeSelf)
                    {                        
                        ObjectPoolManager.Ins.Release(INVITEELEMENT_KEY, gameObject);
                    }
                }
             );
        
    }   

    private void OnDestroy()
    {
        DesubcribeButton();
    }

    async UniTask TimebarRoutine(CancellationToken token)
    {
        try
        {
            float timer = 0f;
            float timeCount = 10f;
            while (timer < timeCount)
            {
                timer += Time.deltaTime;
                timeBar.fillAmount = 1 - (timer / timeCount);
                await UniTask.Yield(token);
            }
            TweenOut();
        }
        catch (OperationCanceledException)
        {

        }
       
    }

    void SubcribeButton()
    {
        accept.onClick.AddListener(OnAccept);
        cancel.onClick.AddListener(OnCancel);
    }

    void DesubcribeButton()
    {
        accept.onClick.RemoveAllListeners();
        cancel.onClick.RemoveAllListeners();
    }

}
