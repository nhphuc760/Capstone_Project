using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class SettingRowItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Preview Info (Hiển thị cột phải khi hover)")]
    public string settingTitle;
    [TextArea(2, 3)] public string settingDescription;
    public Sprite previewIcon;

    [Header("Row Visuals")]
    [SerializeField] private Image rowBgImage;
    [SerializeField] private GameObject highlightFrame; // Khung xanh ngọc + 4 góc bật khi hover
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Colors")]
    [SerializeField] private Color idleBgColor = new Color(0.11f, 0.15f, 0.13f, 1f);    // #1C2620
    [SerializeField] private Color hoverBgColor = new Color(0.06f, 0.35f, 0.27f, 1f);   // #0F5945
    [SerializeField] private Color idleTextColor = new Color(0.85f, 0.85f, 0.82f, 1f);
    [SerializeField] private Color hoverTextColor = new Color(0.96f, 0.96f, 0.94f, 1f);

    public event Action<SettingRowItem> OnRowHovered;

    private void Awake()
    {
        SetHighlight(false);
    }

    private void OnDisable()
    {
        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlight(true);
        OnRowHovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlightFrame != null) highlightFrame.SetActive(active);
        if (rowBgImage != null) rowBgImage.color = active ? hoverBgColor : idleBgColor;
        if (labelText != null) labelText.color = active ? hoverTextColor : idleTextColor;
    }
}