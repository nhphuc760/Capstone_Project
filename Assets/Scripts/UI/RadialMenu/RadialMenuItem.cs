using TMPro;
using UnityEngine;

public class RadialMenuItem : RadialMenuElement
{
    [SerializeField] TextMeshProUGUI amount;
    //[SerializeField] ScriptableObject dataSO;
    public StructureDataSO wallSO;



    protected override void Awake()
    {
        base.Awake();
        icon.sprite = wallSO.icon;
    }

    public void UpdateAmount(int amount)
    {
        if(this.amount)
            this.amount.text = amount.ToString();
    }

}
