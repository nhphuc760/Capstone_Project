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



public class ResourceNode : NetworkBehaviour, ITooltipContext, IInteractable
{   

    [SerializeField] ResourceType resourceType;
    [SerializeField] ToolType requiredTool = ToolType.None;
    [SerializeField] int maxAmount;
    [SerializeField] int amountPerGather;
    [SerializeField] int staminaCostPerGather;
    [SerializeField] float holdDuration = 3f;
    [SerializeField] Animator animator;

    [Tooltip("Phần trăm maxAmount hồi lại sau mỗi đợt wave")] 
    [SerializeField, Range(0f, 1f)] float regenRatePerWave;


    [Header("InteractConfig")]
    [SerializeField] InteractionData interaction;
    [SerializeField] float ratioFrameTarget;
    [Networked] TickTimer InteractionInterval { get; set; }

    public ResourceType ResourceType => resourceType;
    public ToolType RequiredTool => requiredTool;
    public int MaxAmount => maxAmount;  
    public int StaminaCostPerGather => staminaCostPerGather;
    public int RemainingAmount => CurrentAmount;
    [Networked, OnChangedRender(nameof(OnAmountChange))] int CurrentAmount { get; set; }
    public bool IsDepleted => CurrentAmount <= 0;

    public Transform InteractTransform => transform;

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

    public InteractionData Data(NetworkPlayer interactor) => interaction;

    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
       if(interactor.Stamina.CurrentStamina < staminaCostPerGather)
            return new InteractionResult(false, "Không đủ thể lực!");
        if (requiredTool != ToolType.None && interactor.equipTool != requiredTool)
            return new InteractionResult(false, $"Cần {requiredTool.ToString()} để khai thác");
        if (IsDepleted)
            return new InteractionResult(false, "Tài nguyên đã cạn kiệt");
        return new InteractionResult(true);
    }

    public void OnInteract(InteractionContext ctx)
    {
        switch (ctx.Phase)
        {
            case InteractPhase.Press:
               HandlePress(ctx);
                break;
            case InteractPhase.Hold:                
                HandleHold(ctx);
                break;
            case InteractPhase.Release:
                HandleRelease(ctx);
                break;
            case InteractPhase.Cancel:
                break;
            default:
                break;
        }            
    }
    void HandlePress(InteractionContext ctx)
    {       
        float durationAttack = Mathf.Max(0.5f, 1f / ctx.Interactor._stats.Get(StatsType.AttackSpeed));
        float delayEnter = durationAttack * ratioFrameTarget;
        InteractionInterval = TickTimer.CreateFromSeconds(Runner, delayEnter);
    }

    void HandleHold(InteractionContext ctx)
    {
        if (InteractionInterval.ExpiredOrNotRunning(Runner))
        {
            animator?.SetTrigger("Trigger");
            int extractedAmount = Extract();
            ctx.Interactor.AddResource(extractedAmount, resourceType);
            if (Runner.LocalPlayer == ctx.Interactor.Object.InputAuthority)
            {
                var iconTmp = NotificationManager.Ins?.IconTMP;
                NotificationManager.Ins?.ShowNotification($"Đã thu thập {iconTmp.GetResourceCode(ResourceType)} x{extractedAmount}");
            }
            InteractionInterval = TickTimer.CreateFromSeconds(Runner, 1f / ctx.Interactor._stats.Get(StatsType.AttackSpeed));
        }
    }

    void HandleRelease(InteractionContext ctx)
    {

    }

}
