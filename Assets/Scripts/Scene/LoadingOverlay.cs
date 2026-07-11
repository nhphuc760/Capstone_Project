using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LoadingOverlay : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TextMeshProUGUI title;
   public async UniTask FadeInBlack(float duration, string title = null)
    {
        this.title.text = title;
        canvasGroup.blocksRaycasts = true;
        await FadeTo(1f, duration);
    }
    public async UniTask FadeOutBlack(float duration, string title = null)
    {
        this.title.text = title;
        await FadeTo(0f, duration);
        canvasGroup.blocksRaycasts = false;
    }
    async UniTask FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed/duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            await UniTask.Yield();
        }
        canvasGroup.alpha = targetAlpha;
    }
}
