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

    private RectTransform rect;
    private float width;
    private UniTaskCompletionSource<bool> tcs;
    private CancellationTokenSource cts;
    private const string INVITEELEMENT_KEY = "INVITEELEMENT";
    private bool isHandlingResponse = false; // Cờ bảo vệ chống click nhiều lần

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        width = rect.rect.width;
        SubcribeButton();
    }

    private void OnEnable()
    {
        rect.pivot = Vector2.one * .5f;
        rect.anchorMin = new Vector2(1, 0.35f);
        rect.anchorMax = new Vector2(1, 0.35f);
        rect.anchoredPosition = new Vector2(width / 2, 0); // Đặt ở vị trí ẩn ngoài màn hình trước khi TweenIn

        accept.interactable = true;
        cancel.interactable = true;
        isHandlingResponse = false;
        timeBar.fillAmount = 1f;
    }

    private void OnDisable()
    {
        // 1. Dọn dẹp Task đếm ngược
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        // 2. Tránh làm treo Task đang chờ nếu bị disable đột ngột
        if (tcs != null)
        {
            tcs.TrySetResult(false);
            tcs = null;
        }

        // 3. Xóa các Tween đang chạy dở dang trên đối tượng này
        rect.DOKill();
    }

    public UniTask<bool> Show(string titleText, Sprite avt)
    {
        tcs = new UniTaskCompletionSource<bool>();
        avatar.sprite = avt;
        title.text = titleText;

        UpdateInvite();
        return tcs.Task;
    }

    private void OnAccept()
    {
        if (isHandlingResponse) return;
        isHandlingResponse = true;

        accept.interactable = false;
        cancel.interactable = false;

        tcs?.TrySetResult(true);
        tcs = null;

        TweenOut();
    }

    private void OnCancel()
    {
        if (isHandlingResponse) return;
        isHandlingResponse = true;

        accept.interactable = false;
        cancel.interactable = false;

        tcs?.TrySetResult(false);
        tcs = null;

        TweenOut();
    }

    public void UpdateInvite()
    {
        // Dọn dẹp Token cũ trước khi chạy bộ đếm mới
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();

        TimebarRoutine(cts.Token).Forget();
        TweenIn();
    }

    private void TweenIn()
    {
        rect.DOKill(); // Ngắt các tween di chuyển cũ nếu có
        rect.anchoredPosition = new Vector2(width / 2, 0);
        rect.DOAnchorPos(new Vector2(-width / 2, 0), 0.5f)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);
    }

    private void TweenOut()
    {
        // Dừng đếm ngược ngay lập tức khi bắt đầu chạy animation ẩn UI đi
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        rect.DOKill();
        rect.DOAnchorPos(new Vector2(width / 2, 0), 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (gameObject.activeSelf)
                {
                    ObjectPoolManager.Ins.Release(INVITEELEMENT_KEY, gameObject);
                }
            })
            .SetLink(gameObject);
    }

    private void OnDestroy()
    {
        DesubcribeButton();
    }

    private async UniTask TimebarRoutine(CancellationToken token)
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

            // Bảo vệ luồng khi hết giờ đếm ngược
            if (!isHandlingResponse)
            {
                isHandlingResponse = true;
                accept.interactable = false;
                cancel.interactable = false;

                tcs?.TrySetResult(false);
                tcs = null;

                TweenOut();
            }
        }
        catch (OperationCanceledException)
        {
            // Bị hủy chủ động khi click Accept/Cancel hoặc bị Disable -> Hoàn toàn bình thường
        }
    }

    private void SubcribeButton()
    {
        accept.onClick.AddListener(OnAccept);
        cancel.onClick.AddListener(OnCancel);
    }

    private void DesubcribeButton()
    {
        accept.onClick.RemoveAllListeners();
        cancel.onClick.RemoveAllListeners();
    }
}