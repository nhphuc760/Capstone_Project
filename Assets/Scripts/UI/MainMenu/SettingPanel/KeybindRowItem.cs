using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class KeybindRowItem : MonoBehaviour
{
    [Header("Keybind Config")]
    public string actionId = "MoveUp";          // Key lưu PlayerPrefs (vd: MoveUp, Interact, Build)
    public KeyCode defaultKey = KeyCode.W;

    [Header("UI References")]
    [SerializeField] private Button rebindButton;
    [SerializeField] private Image keyCapBg;
    [SerializeField] private TextMeshProUGUI keyText;

    [Header("Colors")]
    [SerializeField] private Color normalKeyBg = new Color(0.14f, 0.20f, 0.17f, 1f);
    [SerializeField] private Color listeningKeyBg = new Color(0.22f, 0.85f, 0.65f, 1f);

    public KeyCode CurrentKey { get; private set; }
    public bool IsListening { get; private set; }

    public static event Action<string, KeyCode> OnKeyRebound;
    private static readonly KeyCode[] AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private int listenStartFrame;

    private void Awake()
    {
        LoadKey();
        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartListening);
    }

    private void Update()
    {
        if (!IsListening || Time.frameCount <= listenStartFrame) return;

        if (Input.anyKeyDown)
        {
            foreach (KeyCode k in AllKeyCodes)
            {
                if (Input.GetKeyDown(k))
                {
                    if (k == KeyCode.Escape)
                    {
                        CancelListening();
                    }
                    else
                    {
                        ApplyNewKey(k);
                    }
                    break;
                }
            }
        }
    }

    public void StartListening()
    {
        IsListening = true;
        listenStartFrame = Time.frameCount;
        if (keyText != null)
        {
            keyText.text = "...";
            keyText.color = Color.black;
        }
        if (keyCapBg != null) keyCapBg.color = listeningKeyBg;
    }

    public void CancelListening()
    {
        IsListening = false;
        RefreshVisual();
    }

    public void ApplyNewKey(KeyCode newKey)
    {
        IsListening = false;
        CurrentKey = newKey;
        PlayerPrefs.SetInt("KeyBind_" + actionId, (int)newKey);
        PlayerPrefs.Save();
        RefreshVisual();
        OnKeyRebound?.Invoke(actionId, newKey);
    }

    public void LoadKey()
    {
        CurrentKey = (KeyCode)PlayerPrefs.GetInt("KeyBind_" + actionId, (int)defaultKey);
        RefreshVisual();
    }

    public void ResetToDefault()
    {
        IsListening = false;
        ApplyNewKey(defaultKey);
    }

    private void RefreshVisual()
    {
        if (keyCapBg != null) keyCapBg.color = normalKeyBg;
        if (keyText != null)
        {
            keyText.color = new Color(0.94f, 0.93f, 0.90f, 1f);
            keyText.text = FormatKeyName(CurrentKey);
        }
    }

    private string FormatKeyName(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => "LMB",
            KeyCode.Mouse1 => "RMB",
            KeyCode.Mouse2 => "MMB",
            KeyCode.LeftShift => "L-SHIFT",
            KeyCode.LeftControl => "L-CTRL",
            KeyCode.Space => "SPACE",
            KeyCode.Return => "ENTER",
            _ => key.ToString().ToUpper()
        };
    }

    // Hàm tiện ích để các script gameplay đọc phím hiện tại bất cứ lúc nào
    public static KeyCode GetBoundKey(string actionId, KeyCode fallback)
    {
        return (KeyCode)PlayerPrefs.GetInt("KeyBind_" + actionId, (int)fallback);
    }
}