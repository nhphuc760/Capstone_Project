using System;
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
    [Tooltip("Phần trăm maxAmount hồi lại sau mỗi đợt wave")] 
    [SerializeField, Range(0f, 1f)] float regenRatePerWave;

    public ResourceType ResourceType => resourceType;
    public ToolType RequiredTool => requiredTool;
    public int MaxAmount => maxAmount;  
    public int StaminaCostPerGather => staminaCostPerGather;
    public int RemainingAmount => CurrentAmount;
    [Networked, OnChangedRender(nameof(OnAmountChange))] int CurrentAmount { get; set; }
    public bool IsDepleted => CurrentAmount <= 0;

    public event Action<ResourceNode> OnDepleted;
    public event Action<ResourceNode> OnRegenerated;
    public event Action<int, int> OnAmountChanged;



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
        if (CurrentAmount <= 0)
        {
            OnDepleted?.Invoke(this);
        }
        return actual;
    }


    public void RegenerateOneWave()
    {
        if (CurrentAmount >= maxAmount) return;
        bool wasDepleted = IsDepleted;
        int regenAmount = Mathf.RoundToInt(maxAmount * regenRatePerWave);
        CurrentAmount = Mathf.Min(maxAmount, CurrentAmount + regenAmount);
        if (wasDepleted && CurrentAmount > 0)
        {
            OnRegenerated?.Invoke(this);
        }
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
