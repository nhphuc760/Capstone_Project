using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text stackText;

    public void SetItem(ItemSO item, int amount)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = item.Icon;

        stackText.text = amount > 1
            ? amount.ToString()
            : "";
    }

    public void Clear()
    {
        itemIcon.enabled = false;
        itemIcon.sprite = null;
        stackText.text = "";
    }
}