using Game.Interaction;
using UnityEngine;

public class PickAxeAttackAction : InteractionAction
{
    [SerializeField] float staminaPerUse = 10f;
    [SerializeField] private ClipTiming _clip = new ClipTiming();
    protected HealthComponent Health { get; private set; }


    protected virtual void Awake() => Health = GetComponentInParent<HealthComponent>();

    private void OnValidate()
    {
        if (Mode != InteractionMode.HoldRepeat)
            Debug.LogWarning($"{name}: cần Mode = HoldRepeat", this);
    }


    protected static float GetAttackSpeed(in InteractionContext ctx)
    {
        var p = ctx.Interactor != null ? ctx.Interactor.GetBehaviour<Stats>() : null;
        return p != null ? Mathf.Max(0.01f, p.Get(StatsType.AttackSpeed)) : 1f;
    }

    public override float GetHoldDuration(in InteractionContext ctx) => _clip.HitDelay(GetAttackSpeed(ctx));
    public override float GetRepeatInterval(in InteractionContext ctx) => ClipTiming.Cycle(GetAttackSpeed(ctx));
    public override float GetAnimationSpeed(in InteractionContext ctx) => _clip.AnimSpeed(GetAttackSpeed(ctx));

    public override bool OnCanInteract(in InteractionContext ctx)
    {
        return ctx.Get<StaminaComponent>().CurrentStamina >= staminaPerUse;
    }

    public override void OnPerform(in InteractionContext ctx)
    {
        if (Health == null || !Health.IsAlive) return;
        var p = ctx.Interactor != null ? ctx.Interactor.GetBehaviour<Stats>() : null;
        float damage  = p != null ? Mathf.Max(0.01f, p.Get(StatsType.Damage)) : 10f;
        Health.TakeDamage(damage, ctx.Interactor.Object);
        ctx.Get<StaminaComponent>().TryConsume((int)staminaPerUse);
    }

    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {
        if (type == InteractionEventType.Perform) OnHitPresentation(ctx, isLocal);
    }

    /// <summary>VFX/SFX/ tại thời điểm trúng. Animation không biết gì về việc này.</summary>
    protected virtual void OnHitPresentation(in InteractionContext ctx, bool isLocal) { }
}
