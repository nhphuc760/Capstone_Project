using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class NotificationItem : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private TMP_Text txtMessage;
    [SerializeField] GameObject successIcon;
    [SerializeField] GameObject WarningIcon;
    [SerializeField] CanvasGroup canvasGroup;
    public RectTransform Rect => rectTransform;

    Tween hideTween;


    private void OnDisable()
    {
        successIcon.SetActive(false);
        WarningIcon.SetActive(false);
    }
    /// <summary>
    /// flags: 0 = null, 1 = success, 2 = warning
    /// </summary>
    /// <param name="message"></param>
    /// <param name="flags"></param>
    public void SetData(string message, int flags = 0)
    {
        if(txtMessage)
        txtMessage.text = message;
        if (flags == 0) return;
        if(flags == 1)
            successIcon.SetActive(true);
        if(flags == 2)
            WarningIcon.SetActive(true);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        hideTween?.Kill();
        canvasGroup.alpha = 1.0f;
        rectTransform.localScale = Vector3.zero;

        rectTransform
            .DOScale(1, 0.35f)
            .SetEase(Ease.OutBack);
    }

    public void UpdateVisual(Vector2 targetPos, float scale, float alpha, float duration)
    {
        rectTransform
            .DOAnchorPos(targetPos, duration)
            .SetEase(Ease.OutCubic);

        rectTransform
            .DOScale(scale, duration)
            .SetEase(Ease.OutCubic);

        canvasGroup.DOFade(alpha, duration);      
    }

    public void StartHide(float delay, Action onComplete)
    {
        hideTween?.Kill();
        hideTween = DOVirtual.DelayedCall(delay, () =>
        {
            Sequence seq = DOTween.Sequence();

            seq.Join(
                canvasGroup.DOFade(0f, .25f)
                );

            seq.Join(
                rectTransform.DOScale(
                    rectTransform.localScale * 0.8f,
                    .25f)
                );

            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        });
    }
    public void CancelHide()
    {
        hideTween?.Kill();
    }

}