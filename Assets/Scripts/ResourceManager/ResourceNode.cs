using System;
using Fusion;
using UnityEngine;


public enum ResourceType
{
    WOOD,
    IRON,
    COPPER,
    GOLD
}

public enum ToolType
{
    None,
    Axe, //Riu - Wood
    PickAxe, // Cuoc - Quang (vang/sat/dong),
    Screwdriver
}



public class ResourceNode : NetworkBehaviour
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
    [Networked] int CurrentAmount { get; set; }
    public bool IsDepleted => CurrentAmount <= 0;

    public event Action<ResourceNode> OnDepleted;
    public event Action<ResourceNode> OnRegenerated;
    public event Action<ResourceNode, int> OnAmountChanged;



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
        OnAmountChanged?.Invoke(this, CurrentAmount);
        Debug.Log($"{resourceType.ToString()} Extract {amountPerGather}, Remaing: {CurrentAmount}");
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
        OnAmountChanged?.Invoke(this, CurrentAmount);

        if (wasDepleted && CurrentAmount > 0)
        {
            OnRegenerated?.Invoke(this);
        }
    }

}
