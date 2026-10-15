using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StructureElementUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] Image hoverImage;
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] GameObject requirementContainer;
    [SerializeField] GameObject EntityValuePrefab;

    private int slotIndex = -1;

    public int SlotIndex => slotIndex;

    public StructureDataSO structureSO;
    BuildUI buildSystemUI;


    public void Initialize(int index, StructureDataSO structureDataSO ,BuildUI buildSystemUI)
    {
        this.slotIndex = index;
        this.structureSO = structureDataSO;
        this.buildSystemUI = buildSystemUI;
        if(Name)
            this.Name.text = structureDataSO._name;
        if (iconImage)
            this.iconImage.sprite = structureDataSO.icon;

        foreach (var i in structureDataSO.buildRequirement)
        {
            if (i is EntityValueBuildRequirement entityValue)
            {
                foreach (var j in entityValue.values)
                {
                    if(j.Value > 0)
                    {
                        var entityObj = Instantiate(EntityValuePrefab, requirementContainer.transform);
                        entityObj.GetComponent<Image>().sprite = j.Icon;
                        entityObj.GetComponentInChildren<TextMeshProUGUI>().text = j.Value.ToString();
                    }
                }

            }
        }

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
        string toolTip = "";
        foreach (var i in structureSO.buildRequirement)
        {
            if(i is EventBuildRequirement eventBuild)
            {
                toolTip += eventBuild.GetInfo(buildSystemUI.System.Object.InputAuthority) + "\n";
            }
        }
        if (!string.IsNullOrEmpty(toolTip))
        {
            //showToolTip

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
