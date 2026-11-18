
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image timeBar;
    [SerializeField] private Image dayNightIcon;

    [Header("Time Bar Colors")]
    [SerializeField] private Color dayColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color nightColor = new Color(0.25f, 0.3f, 0.65f);



    [Header("Day/Night Sprites")]
    [SerializeField] private Sprite sunSprite;
    [SerializeField] private Sprite moonSprite;
    private NetworkTimeManager networkTimeManager;


    private void Awake()
    {
        networkTimeManager = FindAnyObjectByType<NetworkTimeManager>();
    }

    private void Update()
    {
        if (networkTimeManager == null) return;

        UpdateTimeUI();
    }

    private void UpdateTimeUI()
    {
        float currentTime = networkTimeManager.CurrentTimeOfDay;
        bool isDay = networkTimeManager.IsDaytime;

        // Time Text
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);

        timeText.text = $"{hours:00}:{minutes:00}";

        // Time Bar
        timeBar.fillAmount = currentTime / 24f;
        timeBar.color = isDay ? dayColor : nightColor;

        // Day/Night Icon
        dayNightIcon.sprite = isDay ? sunSprite : moonSprite;
    }
}
