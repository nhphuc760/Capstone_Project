using UnityEngine;
using UnityEngine.UI;

public class MenuManagement : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button btnLoadNewGame;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;

    [Header("Panels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelSettings; 

    private void Start()
    {
        if (panelLogin != null) panelLogin.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(false);

        if (btnLoadNewGame != null)
            btnLoadNewGame.onClick.AddListener(OpenLoginPanel);

        if (btnSettings != null)
            btnSettings.onClick.AddListener(OpenSettingsPanel);

        if (btnQuit != null)
            btnQuit.onClick.AddListener(QuitGame);
    }

    private void OnDestroy()
    {
        if (btnLoadNewGame != null)
            btnLoadNewGame.onClick.RemoveListener(OpenLoginPanel);

        if (btnSettings != null)
            btnSettings.onClick.RemoveListener(OpenSettingsPanel);

        if (btnQuit != null)
            btnQuit.onClick.RemoveListener(QuitGame);
    }

    public void OpenLoginPanel()
    {
        if (panelSettings != null) panelSettings.SetActive(false);

        if (panelLogin != null)
        {
            UIHistoryManager.Instance.OpenPanel(panelLogin);
        }
        else
        {
            Debug.LogWarning("[MenuManagement] Chưa kéo Panel_Login vào Inspector!");
        }
    }

    public void OpenSettingsPanel()
    {
        if (panelLogin != null) panelLogin.SetActive(false);

        if (panelSettings != null)
        {
            UIHistoryManager.Instance.OpenPanel(panelSettings);
        }
        else
        {
            Debug.Log("[MenuManagement] Settings Panel chưa được gắn (sẽ bổ sung sau).");
        }
    }

    public void QuitGame()
    {
        Debug.Log("[MenuManagement] Đang thoát game...");

        Application.Quit();

#if UNITY_EDITOR
        // Dừng chế độ Play ngay trong Unity Editor để test nút Quit
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}