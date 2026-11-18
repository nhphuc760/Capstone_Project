using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Menu Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    private bool _isPaused;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        BindSliderEvents();
        SyncSlidersWithSavedData();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        _isPaused = !_isPaused;
        if (pausePanel != null)
            pausePanel.SetActive(_isPaused);

        if (_isPaused)
        {
            SyncSlidersWithSavedData();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            PlayerPrefs.Save();
            // Nếu game khóa chuột khi chơi, mở 2 dòng dưới:
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }
    }

    private void BindSliderEvents()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetVolume(AudioManager.MASTER_PARAM, val));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetVolume(AudioManager.SFX_PARAM, val));

        if (uiSlider != null)
            uiSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetVolume(AudioManager.UI_PARAM, val));
    }

    private void SyncSlidersWithSavedData()
    {
        if (AudioManager.Instance == null) return;

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedVolume(AudioManager.MASTER_PARAM, 1f));

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedVolume(AudioManager.SFX_PARAM, 1f));

        if (uiSlider != null)
            uiSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedVolume(AudioManager.UI_PARAM, 1f));
    }
}