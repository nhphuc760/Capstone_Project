using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathCountdownUI : MonoBehaviour
{
    [SerializeField] private Canvas deathCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image deathOverlay;
    [SerializeField] private float fadeInDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private float fadeTimer;
    private bool isShowing;
   
   

    public void OnDeathStateChanged(bool isDead)
    {
        if (isDead)
        {
            ShowDeathUI();
        }
        else
        {
            HideDeathUI();
        }
    }

    public void OnCountdownUpdated(float remainingTime)
    {
        if (countdownText != null)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            countdownText.text = $"Hồi sinh sau: {seconds - 1}s";
        }
    }

    public void ShowDeathUI()
    {
        if (isShowing) return;

        isShowing = true;
        deathCanvas.gameObject.SetActive(true);
        fadeTimer = 0f;

        if (countdownText != null)
            countdownText.text = "You died!";
    }

    private void HideDeathUI()
    {
        if (!isShowing) return;

        isShowing = false;
        fadeTimer = 0f;
        deathCanvas.gameObject.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (!isShowing || canvasGroup == null) return;

        // Fade in the death UI
        if (fadeTimer < fadeInDuration)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Min(1f, fadeTimer / fadeInDuration);
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
    }
}