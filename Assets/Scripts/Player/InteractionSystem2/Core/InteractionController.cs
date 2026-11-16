using System;
using Fusion;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>Input mẫu. Nếu đã có struct input riêng, override TryReadButtons và bỏ struct này.</summary>
    public struct InteractionInput : INetworkInput
    {
        public NetworkButtons Buttons;
    }

    /// <summary>
    /// Gắn lên prefab người chơi. Là máy trạng thái (Idle / Holding / Session) chạy trên StateAuthority
    /// (Host/Server), client chỉ gửi input và nhận state qua [Networked] để hiển thị.
    /// Core này KHÔNG chứa logic Door/Axe/Radar - chỉ điều phối Action theo Press/Hold/Release.
    /// </summary>
    public class InteractionController : NetworkBehaviour
    {
        [Header("Input (bit trong NetworkButtons)")]
        [SerializeField] private int[] _slotBits = { 0, 1 };   // slot 0 = nút chính, slot 1 = nút phụ// tương ứng với thứ tự enum
        [SerializeField] private int _cancelBit = 2;

        [Header("Detection")]
        [SerializeField] private Transform _origin; // gốc tọa độ của đối Interactor
        [SerializeField] private LayerMask _mask = ~0;
        [SerializeField, Min(0.5f)] private float _searchRadius = 3.5f;
        [SerializeField, Range(-1f, 1f)] private float _minFacingDot = -0.2f;
        [SerializeField] private float _facingWeight = 1.5f;
        [SerializeField] private bool _allowSelf = false;
        [SerializeField, Min(1f)] private float _rangeLeeway = 1.2f; // dung sai khi đang giữ để tránh rớt do lag

        // ---------- Networked state ----------
        [Networked] public InteractionState State { get; set; }
        [Networked] public NetworkId ActiveTarget { get; set; }
        [Networked] public byte ActiveActionIndex { get; set; }
        [Networked] public byte ActiveSlot { get; set; }
        [Tooltip("Dùng để lưu thời điểm bắt đầu cho chế độ HoldRepeat")]
        [Networked] public int StartTick { get; set; }
        [Tooltip("Dùng để lưu Tick Target làm bộ hẹn giờ trong chế độ HoldRepeat")]
        [Networked] public int NextRepeatTick { get; set; }
        [Networked] public float ActiveDuration { get; set; }
        [Networked] public Vector3 StartPosition { get; set; }
        [Networked] public int PerformCounter { get; set; }
        [Networked] public NetworkButtons PrevButtons { get; set; }

        // ---------- Events cho UI / Animator (client-side) ----------
        public event Action<InteractionEvent> Presented;
        public event Action<int, Interactable, InteractionAction> FocusChanged; // chỉ bắn trên máy local

        private static readonly Collider[] Buffer = new Collider[48];
        private IItemHoldProvider _tools;
        private InteractionState _seenState;
        private int _seenPerform;

        private Interactable[] _focusTarget;
        private InteractionAction[] _focusAction;

        private Vector3 Origin => _origin != null ? _origin.position : transform.position;
        private Vector3 Forward => _origin != null ? _origin.forward : transform.forward;
        private ItemSO CurrentTool => _tools?.CurrentHold;

        private void Awake()
        {
            _tools = GetComponentInChildren<IItemHoldProvider>();
            _focusTarget = new Interactable[_slotBits.Length];
            _focusAction = new InteractionAction[_slotBits.Length];
        }
        
        public override void Spawned()
        {
            _seenState = State;
            _seenPerform = PerformCounter;
        }

        // ================= Public API =================

        public bool IsBusy => State != InteractionState.Idle;
        public Interactable GetFocusTarget(int slot) => _focusTarget[slot];
        public InteractionAction GetFocusAction(int slot) => _focusAction[slot];

        /// <summary>Hệ số tốc độ di chuyển hiện tại (Action có thể khoá/giảm tốc). Movement code nhân vào.</summary>
        public float MoveSpeedMultiplier =>
            State != InteractionState.Idle && TryGetActive(out _, out var a) ? a.MoveSpeedMultiplier : 1f;

        public float ActiveProgress01 =>
            State == InteractionState.Holding && ActiveDuration > 0f
                ? Mathf.Clamp01((Runner.Tick.Raw - StartTick) * Runner.DeltaTime / ActiveDuration) : 0f;

        /// <summary>UI (máy local) gọi để gửi dữ liệu (vd: toạ độ bắn pháo). Server kiểm tra rồi chuyển cho Action.OnPayload.</summary>
        public void SendPayload(InteractionAction action, InteractionPayload payload)
        {
            if (!Object.HasInputAuthority || action == null || action.Owner == null) return;
            RPC_SendPayload(action.Owner.Object.Id, action.Index, payload);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SendPayload(NetworkId target, byte actionIndex, InteractionPayload payload)
        {
            if (State == InteractionState.Idle || ActiveTarget != target || ActiveActionIndex != actionIndex) return;
            if (!TryGetActive(out var t, out var a)) return;
            var ctx = MakeContext(t, Elapsed(), 0f);
            a.OnPayload(ctx, payload);
        }

        /// <summary>Override nếu input của bạn nằm trong struct khác.</summary>
        //protected virtual bool TryReadButtons(out NetworkButtons buttons)
        //{
        //    if (GetInput(out InteractionInput input)) { buttons = input.Buttons; return true; }
        //    buttons = default;
        //    return false;
        //}

        //override quick

        protected virtual bool TryReadButtons(out NetworkButtons buttons)
        {
            if (GetInput(out NetworkInputData input)) { buttons = input.button; return true; }
            buttons = default;
            return false;
        }
        // ================= Simulation (StateAuthority) =================

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            TryReadButtons(out var buttons);
            var pressed = buttons.GetPressed(PrevButtons);            
            PrevButtons = buttons;
            bool cancel = pressed.IsSet(_cancelBit);

            switch (State)
            {
                case InteractionState.Idle: TickIdle(pressed); break;
                case InteractionState.Holding: TickHolding(buttons, cancel); break;
                case InteractionState.Session: TickSession(pressed, cancel); break;
            }
        }

        private void TickIdle(NetworkButtons pressed)
        {
            for (int s = 0; s < _slotBits.Length; s++)
            {
                if (!pressed.IsSet(_slotBits[s])) continue;
                if (Resolve(s, out var target, out var action)) { Begin(target, action, s); return; }
            }
        }

        private void Begin(Interactable target, InteractionAction action, int slot)
        {
            ActiveTarget = target.Object.Id;
            ActiveActionIndex = action.Index;
            ActiveSlot = (byte)slot;
            StartTick = Runner.Tick.Raw;
            StartPosition = transform.position;

            var ctx = MakeContext(target, 0f, 0f);
            bool timed = action.Mode == InteractionMode.Hold || action.Mode == InteractionMode.HoldRepeat || action.Mode == InteractionMode.Release;
            ActiveDuration = timed ? Mathf.Max(0.01f, action.GetHoldDuration(ctx)) : 0f;

            action.OnBegin(ctx);

            switch (action.Mode)
            {
                case InteractionMode.Press:
                    Perform(action, ctx);
                    action.OnEnd(ctx, InteractionEndReason.Completed);
                    break;
                case InteractionMode.Hold:
                case InteractionMode.Release:
                    State = InteractionState.Holding;
                    break;
                case InteractionMode.HoldRepeat:
                    State = InteractionState.Holding;
                    NextRepeatTick = StartTick + ToTicks(ActiveDuration);
                    break;
                case InteractionMode.Session:
                    State = InteractionState.Session;
                    break;
            }
        }

        private void TickHolding(NetworkButtons buttons, bool cancel)
        {
            if (!TryGetActive(out var target, out var action)) { State = InteractionState.Idle; return; }

            float elapsed = Elapsed();
            float p = ActiveDuration > 0f ? Mathf.Clamp01(elapsed / ActiveDuration) : 0f;
            var ctx = MakeContext(target, elapsed, p);

            if (cancel) { End(action, ctx, InteractionEndReason.Cancelled); return; }

            if (!buttons.IsSet(_slotBits[ActiveSlot]))
            {
                if (action.Mode == InteractionMode.Release && elapsed >= action.MinHoldTime)
                {
                    Perform(action, ctx);
                    End(action, ctx, InteractionEndReason.Completed);
                }
                else End(action, ctx, InteractionEndReason.Released);
                return;
            }

            if (!ValidateOngoing(target, action, ctx, out var failReason)) { End(action, ctx, failReason); return; }

            action.OnProgress(ctx);

            if (action.Mode == InteractionMode.Hold && elapsed >= ActiveDuration)
            {
                Perform(action, ctx);
                End(action, ctx, InteractionEndReason.Completed);
            }
            else if (action.Mode == InteractionMode.HoldRepeat && Runner.Tick.Raw >= NextRepeatTick)
            {
                NextRepeatTick += ToTicks(action.RepeatInterval);
                Perform(action, ctx);
            }
        }

        private void TickSession(NetworkButtons pressed, bool cancel)
        {
            if (!TryGetActive(out var target, out var action)) { State = InteractionState.Idle; return; }

            var ctx = MakeContext(target, Elapsed(), 0f);

            if (cancel || pressed.IsSet(_slotBits[ActiveSlot])) { End(action, ctx, InteractionEndReason.Cancelled); return; }
            if (!ValidateOngoing(target, action, ctx, out var failReason)) { End(action, ctx, failReason); return; }
            if (!action.OnSessionTick(ctx)) End(action, ctx, InteractionEndReason.Completed);
        }

        private bool ValidateOngoing(Interactable target, InteractionAction action, in InteractionContext ctx, out InteractionEndReason reason)
        {
            reason = InteractionEndReason.Completed;

            if (FlatDistance(Origin, target.FocusPoint.position) > action.EffectiveRange * _rangeLeeway)
            { reason = InteractionEndReason.OutOfRange; return false; }

            if (action.CancelOnMove && action.MoveSpeedMultiplier > 0f)
            {
                var d = transform.position - StartPosition; d.y = 0f;
                if (d.sqrMagnitude > action.MoveTolerance * action.MoveTolerance)
                { reason = InteractionEndReason.Moved; return false; }
            }

            if (!action.CanInteract(ctx)) { reason = InteractionEndReason.ConditionFailed; return false; }
            return true;
        }

        private void Perform(InteractionAction action, in InteractionContext ctx)
        {
            action.OnPerform(ctx);
            PerformCounter++;
        }

        private void End(InteractionAction action, in InteractionContext ctx, InteractionEndReason reason)
        {
            action.OnEnd(ctx, reason);
            State = InteractionState.Idle;
        }

        // ================= Target resolution (dùng chung server + client prompt) =================
        /// <summary>
        /// Kiểm tra 1 cuộc tương tác có đạt điều kiện không ? Đồng thời trả về action phù hợp nhất
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool Resolve(int slot, out Interactable target, out InteractionAction action)
        {
            target = null; action = null;
            float best = float.MaxValue;

            var origin = Origin;
            var fwd = Forward; fwd.y = 0f; fwd.Normalize();
            int n = Physics.OverlapSphereNonAlloc(origin, _searchRadius, Buffer, _mask, QueryTriggerInteraction.Collide); // số các đối tượng trong phạm vi

            for (int i = 0; i < n; i++)
            {
                var it = Buffer[i].GetComponentInParent<Interactable>();
                if (it == null || it.Object == null || !it.Object.IsValid) continue;
                if (!_allowSelf && it.Object == Object) continue;

                var to = it.FocusPoint.position - origin; to.y = 0f;
                float d = to.magnitude;
                float dot = d > 0.01f ? Vector3.Dot(fwd, to / d) : 1f;
                if (dot < _minFacingDot) continue;

                var ctx = MakeContext(it, 0f, 0f);
                if (!it.TryGetBestAction(slot, ctx, d, out var a)) continue;

                float score = d - dot * _facingWeight;
                if (score < best) { best = score; target = it; action = a; }
            }
            return action != null;
        }

        // ================= Presentation (mọi client) =================

        public override void Render()
        {
            var st = State;
            if (_seenState == InteractionState.Idle && st != InteractionState.Idle) Raise(InteractionEventType.Begin);
            if (PerformCounter != _seenPerform) { _seenPerform = PerformCounter; Raise(InteractionEventType.Perform); }
            if (_seenState != InteractionState.Idle && st == InteractionState.Idle) Raise(InteractionEventType.End);
            _seenState = st;

            if (Object.HasInputAuthority) UpdateFocus();
        }

        private void Raise(InteractionEventType type)
        {
            if (!TryGetActive(out var t, out var a)) return;
            float e = Elapsed();
            var ctx = MakeContext(t, e, ActiveDuration > 0f ? Mathf.Clamp01(e / ActiveDuration) : 0f);
            bool local = Object.HasInputAuthority;
            a.OnPresentation(type, ctx, local);
            Presented?.Invoke(new InteractionEvent(type, ctx, a, local));
        }

        private void UpdateFocus()
        {
            for (int s = 0; s < _slotBits.Length; s++)
            {
                Interactable t = null; InteractionAction a = null;
                if (State == InteractionState.Idle) Resolve(s, out t, out a);
                if (t != _focusTarget[s] || a != _focusAction[s])
                {
                    _focusTarget[s] = t; _focusAction[s] = a;
                    FocusChanged?.Invoke(s, t, a);
                }
            }
        }

        // ================= Helpers =================

        /// <summary>
        /// Thử lấy đối target có thể tương tác
        /// </summary>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool TryGetActive(out Interactable target, out InteractionAction action)
        {
            target = null; action = null;
            if (!ActiveTarget.IsValid || !Runner.TryFindObject(ActiveTarget, out var no)) return false;
            target = no.GetComponentInChildren<Interactable>();
            if (target == null) return false;
            action = target.GetAction(ActiveActionIndex);
            return action != null;
        }

        /// <summary>
        /// Tạo Context 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="elapsed"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private InteractionContext MakeContext(Interactable target, float elapsed, float progress) => new InteractionContext
        {
            Interactor = this, Target = target, ItemHold = CurrentTool, Elapsed = elapsed, Progress01 = progress
        };

        private float Elapsed() => (Runner.Tick.Raw - StartTick) * Runner.DeltaTime;
        /// <summary>
        /// Chuyển đổi thời gian về số tick mạng tương đương
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private int ToTicks(float seconds) => Mathf.Max(1, Mathf.CeilToInt(seconds / Runner.DeltaTime));

        /// <summary>
        /// Tính khoảng cách trên mặt phẳng XZ
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
