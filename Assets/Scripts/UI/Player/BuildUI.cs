using UnityEngine;

public class BuildUI : MonoBehaviour, IPlayerUI
{

    [SerializeField] BuildSystem buildSystem;
    [SerializeField] StructDatabase structureDatabaseSO;
    

    private void Awake()
    {
        GetComponentInParent<PlayerUIComponent>().RegisterPlayerUI(PlayerUIComponent.OpenUI.BuildMenu, this);
        gameObject.SetActive(false);
    }
    public void OnSlotClick(StructureElementUI element)
    {
        foreach (var i in element.structureSO.buildRequirement)
        {
            var check = i.CheckRequirement(buildSystem.Object.Runner, buildSystem.Object.InputAuthority);
            if (check.Success)
            {
                Close();
            }
            else
            {
                //Announce
                return;
            }
        }
        buildSystem.PickStruct(element.structureSO._id.Value);
    }         

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }
}
