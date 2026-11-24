using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ChooseTypeNotificationUI : MonoBehaviour
{
    [Header("Tham chiếu UI Toast")]
    [SerializeField] private CanvasGroup toastCanvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Thời gian hiệu ứng (giây)")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float displayDuration = 3.5f;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.interactable = false;
            toastCanvasGroup.blocksRaycasts = false;
            toastCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(LobbyReadyManager.PendingDisconnectNotice))
        {
            string msg = LobbyReadyManager.PendingDisconnectNotice;
            LobbyReadyManager.PendingDisconnectNotice = null; 
            ShowToast(msg);
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void ShowToast(string message)
    {
        if (toastCanvasGroup == null) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (messageText != null)
            messageText.text = message;

        AnimateToastAsync(_cts.Token).Forget();
    }

    private async UniTaskVoid AnimateToastAsync(CancellationToken token)
    {
        toastCanvasGroup.gameObject.SetActive(true);
        toastCanvasGroup.blocksRaycasts = false;

        try
        {
            // 1. Hiện dần (Fade In)
            float t = 0f;
            while (t < fadeDuration)
            {
                token.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            toastCanvasGroup.alpha = 1f;

            // 2. Giữ thông báo trên màn hình
            await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), DelayType.UnscaledDeltaTime, cancellationToken: token);

            // 3. Mờ dần (Fade Out)
            t = 0f;
            while (t < fadeDuration)
            {
                token.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            // Bỏ qua khi chuyển scene giữa chừng
        }
    }
}