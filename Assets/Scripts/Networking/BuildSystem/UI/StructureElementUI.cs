using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StructureElementUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] Image hoverImage;

    private int slotIndex = -1;

    public int SlotIndex => slotIndex;


    public StructureDataSO structureSO;
    BuildUI buildSystemUI;


    public void Initialize(int index, StructureDataSO structureDataSO ,BuildUI buildSystemUI)
    {
        this.slotIndex = index;
        this.structureSO = structureDataSO;
        this.buildSystemUI = buildSystemUI;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            buildSystemUI.OnSlotClick(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverImage)
        {
            hoverImage.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverImage)
        {
            hoverImage.enabled = false;
        }
    }
   





}
