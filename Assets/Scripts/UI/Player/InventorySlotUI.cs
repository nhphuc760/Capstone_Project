using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] Image hoverHighlight;
    [SerializeField] Image selectedHighlight;
    private InventoryItem currentItem;
    private ItemSO currentData;


    private int slotIndex = -1;

    public int SlotIndex => slotIndex;
    public InventoryItem CurrentItem => currentItem;
    public ItemSO CurrentData => currentData;
    public bool IsEmpty => currentItem.IsEmpty;

    InventoryUI inventoryUI;
    bool isSelected = false;

    public void Initialize(int index, InventoryUI inventoryUI)
    {
        slotIndex = index;
        this.inventoryUI = inventoryUI;
        if (hoverHighlight) hoverHighlight.enabled = false;
        if(selectedHighlight) selectedHighlight.enabled = false;
        Clear();
    }

    public void SetItem(InventoryItem item, ItemSO data)
    {
        currentItem = item;
        currentData = data;

        if (item.IsEmpty || data == null)
        {
            Clear();
            return;
        }

        // Icon
        if (iconImage != null)
        {            
            iconImage.sprite = data.Icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
        }

        // Amount
        if (amountText != null)
        {
            if (data.Stackable && item.amount > 1)
            {
                amountText.text = item.amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.text = "";
                amountText.enabled = false;
            }
        }
       
    }

    public void Clear()
    {
        currentItem = default;
        currentData = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.enabled = false;
        }        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!isSelected && hoverHighlight != null)
        {
            hoverHighlight.enabled = true;
        }
        if (inventoryUI != null)
        {
            inventoryUI.ShowTooltip(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventoryUI.OnSlotClicked(this);
        }else if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryUI.DeselectSlot();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(hoverHighlight != null)
        {
            hoverHighlight.enabled = false;
        }
        if (inventoryUI != null)
        {
            inventoryUI.HideTooltip();
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedHighlight != null)
        {
            selectedHighlight.enabled = selected;
        }

        // Khi được chọn thì tắt hover highlight
        if (selected && hoverHighlight != null)
        {
            hoverHighlight.enabled = false;
        }
    }
}