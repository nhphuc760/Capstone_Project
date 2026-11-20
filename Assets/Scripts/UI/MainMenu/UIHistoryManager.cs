using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class UIHistoryManager : MonoBehaviour
{
    public static UIHistoryManager Instance { get; private set; }

    private class PanelEntry
    {
        public GameObject panel;
        public bool hidPrevious;
    }

    private readonly Stack<PanelEntry> historyStack = new Stack<PanelEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (IsEscapePressed())
        {
            GoBack();
        }
    }

    /// <summary>
    /// Bật Panel đè lên trên (Dùng cho: Title -> Login, Title -> Settings).
    /// </summary>
    public void OpenPanel(GameObject panel)
    {
        PushPanel(panel, hidePrevious: false);
    }

    /// <summary>
    /// Tạm tắt Panel hiện tại và bật Panel mới; bấm ESC sẽ bật lại Panel cũ (Dùng cho: Login -> Forgot Password).
    /// </summary>
    public void OpenPanelExclusive(GameObject panel)
    {
        PushPanel(panel, hidePrevious: true);
    }

    private void PushPanel(GameObject panel, bool hidePrevious)
    {
        if (panel == null) return;

        CleanupInactiveTop();

        if (historyStack.Count > 0 && historyStack.Peek().panel == panel)
            return;

        if (hidePrevious && historyStack.Count > 0)
        {
            PanelEntry currentTop = historyStack.Peek();
            if (currentTop.panel != null)
                currentTop.panel.SetActive(false);
        }

        panel.SetActive(true);

        historyStack.Push(new PanelEntry
        {
            panel = panel,
            hidPrevious = hidePrevious
        });
    }

    /// <summary>
    /// Tắt Panel hiện tại và trả lại bước trước đó.
    /// </summary>
    public void GoBack()
    {
        CleanupInactiveTop();

        if (historyStack.Count == 0) return;

        PanelEntry top = historyStack.Pop();

        if (top.panel != null)
            top.panel.SetActive(false);

        if (top.hidPrevious && historyStack.Count > 0)
        {
            PanelEntry previous = historyStack.Peek();
            if (previous.panel != null)
                previous.panel.SetActive(true);
        }
    }

    /// <summary>
    /// Tắt toàn bộ Panel (Dùng khi đăng nhập thành công để chuyển sang màn chọn chế độ).
    /// </summary>
    public void CloseAll()
    {
        while (historyStack.Count > 0)
        {
            PanelEntry entry = historyStack.Pop();
            if (entry.panel != null)
                entry.panel.SetActive(false);
        }
    }

    private void CleanupInactiveTop()
    {
        while (historyStack.Count > 0)
        {
            PanelEntry top = historyStack.Peek();
            if (top.panel == null || !top.panel.activeInHierarchy)
                historyStack.Pop();
            else
                break;
        }
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}