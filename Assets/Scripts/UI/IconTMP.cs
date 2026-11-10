using UnityEngine;


[CreateAssetMenu(fileName = "IconTMP", menuName = "TextMeshPro/IconTMP_SO")]
public class IconTMP : ScriptableObject
{
    [Header("Resources")]
    [SerializeField] string woodCode;
    [SerializeField] string stoneCode;
    [SerializeField] string goldCode;
    [SerializeField] string electricCode;
    [SerializeField] string axe;
    [SerializeField] string pickAxe;
    public string WoodCode => woodCode;
    public string StoneCode => stoneCode;
    public string GoldCode => goldCode;
    public string ElectricCode => electricCode;
    public string Axe => axe;
    public string PickAxe => pickAxe;   


    public string GetResourceCode(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Wood => woodCode,
            ResourceType.Stone => stoneCode,
            ResourceType.Gold => goldCode,
            ResourceType.Electric => electricCode,
            _ => null
        };
    }

    public string GetToolCode(ToolType toolType)
    {
        return toolType switch 
        {
            ToolType.Axe => axe,
            ToolType.PickAxe => pickAxe,
            _ => null
        };
    }


}
