using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System;

public class SettingsPanelManager : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject settingsRootPanel;

    [Header("Tabs")]
    [SerializeField] private Button btnTabPreferences;
    [SerializeField] private Button btnTabController;
    [SerializeField] private TextMeshProUGUI txtTabPreferences;
    [SerializeField] private TextMeshProUGUI txtTabController;
    [SerializeField] private GameObject panelPreferencesContent;
    [SerializeField] private GameObject panelControllerContent;

    [Header("Tab Colors")]
    [SerializeField] private Color activeTabColor = new Color(0.22f, 0.96f, 0.72f, 1f);  // #39F5B8
    [SerializeField] private Color inactiveTabColor = new Color(0.86f, 0.85f, 0.82f, 1f);

    [Header("Audio Mixer & Sliders")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private TextMeshProUGUI txtMasterPercent;
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private TextMeshProUGUI txtMusicPercent;
    [SerializeField] private Slider sliderVFX;
    [SerializeField] private TextMeshProUGUI txtVFXPercent;

    [Header("Language Selector")]
    [SerializeField] private Button btnLangPrev;
    [SerializeField] private Button btnLangNext;
    [SerializeField] private TextMeshProUGUI txtCurrentLanguage;
    [SerializeField] private string[] supportedLanguages = { "English", "Tiếng Việt" };

    [Header("Right Preview Panel")]
    [SerializeField] private Image imgPreviewIcon;
    [SerializeField] private TextMeshProUGUI txtPreviewTitle;
    [SerializeField] private TextMeshProUGUI txtPreviewDesc;

    [Header("Footer Buttons")]
    [SerializeField] private Button btnRestoreDefaults;
    [SerializeField] private Button btnBack;

    [Header("Row Items")]
    [SerializeField] private SettingRowItem[] allSettingRows;
    [SerializeField] private KeybindRowItem[] allKeybindRows;

    public static event Action<int> OnLanguageChanged;

    private int currentTabIndex = 0; // 0 = Preferences, 1 = Controller
    private int currentLangIndex = 0;

    private void Start()
    {
        // Gắn sự kiện chuyển Tab
        if (btnTabPreferences != null) btnTabPreferences.onClick.AddListener(() => SwitchTab(0));
        if (btnTabController != null) btnTabController.onClick.AddListener(() => SwitchTab(1));

        // Gắn sự kiện Hover cho tất cả các hàng để cập nhật khung xem trước bên phải
        foreach (var row in allSettingRows)
        {
            if (row != null) row.OnRowHovered += UpdatePreviewPanel;
        }

        // Gắn sự kiện Slider âm lượng
        if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(SetMasterVolume);
        if (sliderMusic != null) sliderMusic.onValueChanged.AddListener(SetMusicVolume);
        if (sliderVFX != null) sliderVFX.onValueChanged.AddListener(SetVFXVolume);

        // Gắn sự kiện đổi ngôn ngữ
        if (btnLangPrev != null) btnLangPrev.onClick.AddListener(() => CycleLanguage(-1));
        if (btnLangNext != null) btnLangNext.onClick.AddListener(() => CycleLanguage(1));

        // Nút khôi phục mặc định & đóng bảng
        if (btnRestoreDefaults != null) btnRestoreDefaults.onClick.AddListener(RestoreAllDefaults);
        if (btnBack != null) btnBack.onClick.AddListener(CloseSettings);

        LoadSavedPreferences();
        SwitchTab(0);
    }

    private void Update()
    {
        if (settingsRootPanel == null || !settingsRootPanel.activeSelf) return;

        // Nếu đang chờ người chơi bấm phím mới thì không xử lý phím tắt của panel
        foreach (var kb in allKeybindRows)
        {
            if (kb != null && kb.IsListening) return;
        }

        // Phím Q / E chuyển nhanh giữa 2 tab giống ZL / ZR trong mẫu
        if (Input.GetKeyDown(KeyCode.Q)) SwitchTab(0);
        else if (Input.GetKeyDown(KeyCode.E)) SwitchTab(1);
        else if (Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
    }

    public void OpenSettings()
    {
        if (settingsRootPanel != null) settingsRootPanel.SetActive(true);
        SwitchTab(0);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        GameObject target = settingsRootPanel != null ? settingsRootPanel : gameObject;
        if (UIHistoryManager.Instance != null)
            UIHistoryManager.Instance.ClosePanel(target);
        else
            target.SetActive(false);
    }

    public void SwitchTab(int tabIndex)
    {
        currentTabIndex = tabIndex;
        bool isPref = (tabIndex == 0);

        if (panelPreferencesContent != null) panelPreferencesContent.SetActive(isPref);
        if (panelControllerContent != null) panelControllerContent.SetActive(!isPref);

        if (txtTabPreferences != null)
        {
            txtTabPreferences.text = isPref ? "Preferences" : "Preferences";
            txtTabPreferences.color = isPref ? activeTabColor : inactiveTabColor;
        }

        if (txtTabController != null)
        {
            txtTabController.text = !isPref ? "Controller" : "Controller";
            txtTabController.color = !isPref ? activeTabColor : inactiveTabColor;
        }

        // Hiển thị mặc định dòng đầu tiên của tab đang mở lên khung bên phải
        foreach (var row in allSettingRows)
        {
            if (row != null && row.gameObject.activeInHierarchy)
            {
                UpdatePreviewPanel(row);
                break;
            }
        }
    }

    private void UpdatePreviewPanel(SettingRowItem item)
    {
        if (txtPreviewTitle != null) txtPreviewTitle.text = item.settingTitle;
        if (txtPreviewDesc != null) txtPreviewDesc.text = item.settingDescription;
        if (imgPreviewIcon != null && item.previewIcon != null)
            imgPreviewIcon.sprite = item.previewIcon;
    }

    #region Audio & Language
    public void SetMasterVolume(float value)
    {
        ApplyMixerVolume("MasterVolume", value);
        PlayerPrefs.SetFloat("Opt_MasterVol", value);
        if (txtMasterPercent != null) txtMasterPercent.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetMusicVolume(float value)
    {
        ApplyMixerVolume("BGMVolume", value);
        PlayerPrefs.SetFloat("Opt_MusicVol", value);
        if (txtMusicPercent != null) txtMusicPercent.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetVFXVolume(float value)
    {
        ApplyMixerVolume("VFXVolume", value);
        PlayerPrefs.SetFloat("Opt_VFXVol", value);
        if (txtVFXPercent != null) txtVFXPercent.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void ApplyMixerVolume(string exposedParam, float normalizedValue)
    {
        if (mainAudioMixer == null) return;
        float dB = normalizedValue <= 0.0001f ? -80f : Mathf.Log10(normalizedValue) * 20f;
        mainAudioMixer.SetFloat(exposedParam, dB);
    }

    private void CycleLanguage(int direction)
    {
        if (supportedLanguages == null || supportedLanguages.Length == 0) return;
        currentLangIndex = (currentLangIndex + direction + supportedLanguages.Length) % supportedLanguages.Length;
        ApplyLanguage(currentLangIndex);
    }

    private void ApplyLanguage(int index)
    {
        currentLangIndex = Mathf.Clamp(index, 0, supportedLanguages.Length - 1);
        if (txtCurrentLanguage != null) txtCurrentLanguage.text = supportedLanguages[currentLangIndex];
        PlayerPrefs.SetInt("Opt_Language", currentLangIndex);
        OnLanguageChanged?.Invoke(currentLangIndex);
    }
    #endregion

    private void LoadSavedPreferences()
    {
        float master = PlayerPrefs.GetFloat("Opt_MasterVol", 1f);
        float music = PlayerPrefs.GetFloat("Opt_MusicVol", 1f);
        float vfx = PlayerPrefs.GetFloat("Opt_VFXVol", 1f);
        int lang = PlayerPrefs.GetInt("Opt_Language", 0);

        if (sliderMaster != null) sliderMaster.value = master;
        if (sliderMusic != null) sliderMusic.value = music;
        if (sliderVFX != null) sliderVFX.value = vfx;

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetVFXVolume(vfx);
        ApplyLanguage(lang);
    }

    public void RestoreAllDefaults()
    {
        if (currentTabIndex == 0)
        {
            if (sliderMaster != null) sliderMaster.value = 1f;
            if (sliderMusic != null) sliderMusic.value = 1f;
            if (sliderVFX != null) sliderVFX.value = 1f;
            ApplyLanguage(0);
        }
        else
        {
            foreach (var kb in allKeybindRows)
            {
                if (kb != null) kb.ResetToDefault();
            }
        }
        PlayerPrefs.Save();
    }
}