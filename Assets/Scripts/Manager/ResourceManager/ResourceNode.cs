using System;
using System.Linq;
using Fusion;
using UnityEngine;


public enum ResourceType
{
    Wood,
    Stone,
    Gold,
    Electric
}

public enum ToolType
{
    None,
    Axe, //Riu - Wood
    PickAxe, // Cuoc - Quang (vang/sat/dong),
    Screwdriver
}



public class ResourceNode : NetworkBehaviour, ITooltipContext
{   

    [SerializeField] ResourceType resourceType;
    [SerializeField] ToolType requiredTool = ToolType.None;
    [SerializeField] int maxAmount;
    [SerializeField] int amountPerGather;
    [SerializeField] int staminaCostPerGather;
    [SerializeField] float holdDuration = 3f;
    [SerializeField] Animator animator;
    public IconTMP iconTMP;

    [Tooltip("Phần trăm maxAmount hồi lại sau mỗi đợt wave")] 
    [SerializeField, Range(0f, 1f)] float regenRatePerWave;


    [Header("InteractConfig")]
    public ResourceType ResourceType => resourceType;
    public ToolType RequiredTool => requiredTool;
    public int MaxAmount => maxAmount;  
    public int StaminaCostPerGather => staminaCostPerGather;
    public int RemainingAmount => CurrentAmount;
    [Networked, OnChangedRender(nameof(OnAmountChange))] int CurrentAmount { get; set; }
    public bool IsDepleted => CurrentAmount <= 0;




    public event Action<int, int> OnAmountChanged;


    private void Awake()
    {
        if (iconTMP == null)
        {
            Debug.LogWarning("iconTMP null");
            iconTMP = Resources.LoadAll<IconTMP>("IconTextMeshPro").First();
        }
    }


    public override void Spawned()
    {
        CurrentAmount = maxAmount;
    }

    public int Extract(float efficiencyMultiplier = 1f)
    {

        if (IsDepleted) return 0;
        int requested = Mathf.RoundToInt(amountPerGather * Mathf.Max(0f, efficiencyMultiplier));
        int actual = Mathf.Min(requested, CurrentAmount);
        CurrentAmount -= actual;               
        return actual;
    }    


    void OnAmountChange()
    {
        OnAmountChanged?.Invoke(CurrentAmount, maxAmount);
    }

    public string GetContent()
    {
        return $"{ResourceType.ToString()}\n" +
            $"{CurrentAmount}/{MaxAmount}";
    }   
}
