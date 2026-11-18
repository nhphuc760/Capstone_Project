using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component tooltip tái sử dụng được ở nhiều nơi (Inventory, ToolKit, ...).
/// Gắn vào một GameObject chứa panel + TextMeshProUGUI.
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(130f, -50f, 0f);
    [SerializeField] private bool followMouse = true;

    private RectTransform panelRect;

    private void Awake()
    {
        if (tooltipPanel != null)
            panelRect = tooltipPanel.GetComponent<RectTransform>();

        Hide();
    }

    private void Update()
    {
        if (followMouse && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.transform.position = Input.mousePosition + offset;
        }
    }

    public void Show(ItemSO itemSO, int amount = 1)
    {
        if (tooltipPanel == null || itemSO == null)
        {
            Hide();
            return;
        }

        string name = itemSO.ItemName;
        string desc = itemSO.Description;

        if (string.IsNullOrEmpty(desc))
        {
            tooltipText.text = $"<b><size=110%>{name}</size></b>";
        }
        else
        {
            tooltipText.text = $"<b><size=110%>{name}</size></b>\n{desc}";
        }

        // Nếu stackable và amount > 1 thì hiện thêm số lượng (tuỳ chọn)
        if (itemSO.Stackable && amount > 1)
        {
            tooltipText.text += $"\n<color=#AAAAAA>x{amount}</color>";
        }

        tooltipPanel.SetActive(true);

        if (panelRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        if (followMouse)
            tooltipPanel.transform.position = Input.mousePosition + offset;
    }

    public void Show(InventoryItem item, ItemSO data)
    {
        if (item.IsEmpty || data == null)
        {
            Hide();
            return;
        }

        Show(data, item.amount);
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public bool IsVisible => tooltipPanel != null && tooltipPanel.activeSelf;
}
