using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildCategoryButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] TextMeshProUGUI text;
    BuildUI buildUI;
    StructureCategory category;

    public StructureCategory Category => category;

    public void Initial(StructureCategory category, BuildUI buildUI)
    {
        this.category = category;
        this.buildUI = buildUI;
        this.text.text = category.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            buildUI.OnCategoryButtonClick(this);
        }
    }
}
