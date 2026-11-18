using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Base cho hành động HoldRepeat theo frame clip.
    /// Nhát đầu: sau HitDelay (Begin -> HitFrame). Các nhát sau: mỗi Cycle (1 vòng clip).
    /// Cả hai đều chia cho attackSpeed của Interactor.
    /// Trong Inspector: Mode = HoldRepeat, Presentation.LoopBool = bool loop animation chặt/đào.
    /// </summary>
    public abstract class GatherAction : InteractionAction
    {
        [Header("Gather")]
        [SerializeField] private ClipTiming _clip = new ClipTiming();
        protected ResourceNode Node { get; private set; }

        protected virtual void Awake() => Node = GetComponentInParent<ResourceNode>();

        private void OnValidate()
        {
            if (Mode != InteractionMode.HoldRepeat)
                Debug.LogWarning($"{name}: GatherAction cần Mode = HoldRepeat", this);
        }

        // ---------- Thời gian ----------
        protected static float GetAttackSpeed(in InteractionContext ctx)
        {
            var p = ctx.Interactor != null ? ctx.Interactor.GetBehaviour<Stats>() : null;
            return p != null ? Mathf.Max(0.01f, p.Get(StatsType.AttackSpeed)) : 1f;
        }

        public override float GetHoldDuration(in InteractionContext ctx) => _clip.HitDelay(GetAttackSpeed(ctx));
        public override float GetRepeatInterval(in InteractionContext ctx) => ClipTiming.Cycle(GetAttackSpeed(ctx));
        public override float GetAnimationSpeed(in InteractionContext ctx) => _clip.AnimSpeed(GetAttackSpeed(ctx));       
        // ---------- Logic ----------
        public override bool OnCanInteract(in InteractionContext ctx)
            => Node != null && !Node.IsDepleted && CanHarvest(ctx);

        protected virtual bool CanHarvest(in InteractionContext ctx) => true;

        public override void OnPerform(in InteractionContext ctx)
        {
            if (Node == null || Node.IsDepleted) return;
            int extract = Node.Extract();
            ctx.Get<NetworkPlayer>().AddResource(extract, Node.ResourceType);
            if(Node.IsDepleted)
                OnDepleted(ctx);
        }
        /// <summary>Server: spawn loot / cộng item cho ctx.Player ở đây.</summary>
        protected virtual void OnDepleted(in InteractionContext ctx) { }

        // ---------- Presentation (mọi client, bắn đúng thời điểm nhát chặt) ----------
        public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
        {
            if (type == InteractionEventType.Perform) OnHitPresentation(ctx, isLocal);
        }

        /// <summary>VFX/SFX/rung cây tại thời điểm trúng. Animation không biết gì về việc này.</summary>
        protected virtual void OnHitPresentation(in InteractionContext ctx, bool isLocal) { }
        
    }
}