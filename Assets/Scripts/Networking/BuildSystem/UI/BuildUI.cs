using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class BuildUI : MonoBehaviour, IPlayerUI
{

    [SerializeField] ScrollRect scrollView;
    [SerializeField] GameObject scrollviewContainer;
    [SerializeField] StructureElementUI elementUI;
    [SerializeField] BuildCategoryButton buildCategoryButtonPrefab;
    [SerializeField] RectTransform buttonCategoryContainer;
    [SerializeField] BuildSystem buildSystem;
    [SerializeField] StructDatabase structureDatabaseSO;
    
    Dictionary<BuildCategoryButton, ScrollRect> keyValuePairs = new Dictionary<BuildCategoryButton, ScrollRect>();
    BuildCategoryButton _curCategorySelect;

    public BuildSystem System => buildSystem;

    private void Awake()
    {
        GetComponentInParent<PlayerUIComponent>().RegisterPlayerUI(PlayerUIComponent.OpenUI.BuildMenu, this);
        Initialize();
        Close();
    }



    void Initialize()
    {
        foreach (var i in Enum.GetValues(typeof(StructureCategory)).Cast<StructureCategory>())
        {
            if (i == StructureCategory.None) continue;
            BuildCategoryButton buttonCategory = Instantiate(buildCategoryButtonPrefab, buttonCategoryContainer);
            buttonCategory.Initial(i, this);
            var scrollObj = Instantiate(scrollView, scrollviewContainer.transform);
            keyValuePairs.Add(buttonCategory, scrollObj);
            var structureCategoryI = structureDatabaseSO.StructureList.Where(x => x.structureCategory == i);
            for (int j = 0; j < structureCategoryI.Count(); j ++)
            {
                var elementStructure = Instantiate(elementUI, scrollObj.content);
                elementStructure.Initialize(j, structureCategoryI.ElementAt(j), this);
            }  
            scrollObj.gameObject.SetActive(false);
        }

        var first = keyValuePairs.FirstOrDefault();
        if (first.Value)
        {
            OnCategoryButtonClick(first.Key);
        }

    }


    public void OnCategoryButtonClick(BuildCategoryButton button)
    {
        if (keyValuePairs.TryGetValue(button, out var obj))
        {
            if(_curCategorySelect != null)
            keyValuePairs[_curCategorySelect].gameObject.SetActive(false);
            _curCategorySelect = button;
            keyValuePairs[_curCategorySelect].gameObject.SetActive(true);
        }
    }

    public void OnSlotClick(StructureElementUI element)
    {
        StructureDataSO so = element.structureSO;
        if (so.buildRequirement == null || so.buildRequirement.Length == 0) return;
        foreach (var i in element.structureSO.buildRequirement)
        {
            var check = i.CheckRequirement(buildSystem.Object.Runner, buildSystem.Object.InputAuthority);
            if (!check.Success)
            {
                //Announce
                Debug.Log(check.Message);
                return;
               
            }            
        }
        CancelButton();
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

    public void CancelButton()
    {
        GetComponentInParent<PlayerUIComponent>().Toggle(PlayerUIComponent.OpenUI.BuildMenu);
    }

}
