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
    /// 
    public class InteractionController : NetworkBehaviour
    {
        [Header("Input (bit trong NetworkButtons)")]
        [SerializeField] private int[] _slotBits = { 0, 1 };   // slot 0 = nút chính, slot 1 = nút phụ// tương ứng với thứ tự enum
        [SerializeField] private int _cancelBit = 2;

        [Header("Detection")]
        [SerializeField] private Transform _origin; // gốc tọa độ của đối Interactor
        [SerializeField] private bool _allowSelf = false;
        [SerializeField, Min(0f)] private float _rangeLeeway = 0.2f; // dung sai khi đang giữ để tránh rớt do lag

        [Header("References")]
        [SerializeField] SuggestTarget suggestTarget;
        [SerializeField] GameObject modelPlayer;

        // ---------- Networked state ----------
        [Tooltip("Trạng thái thực của phiên Interaction")]
        [Networked] public InteractionState State { get; set; }
        [Tooltip("Target thực")]
        [Networked] public NetworkId ActiveTarget { get; set; }

        [Networked] public byte ActiveActionIndex { get; set; }

        [Tooltip("Lưu input hiện tại đang bấm")]
        [Networked] public byte ActiveSlot { get; set; }
        [Tooltip("Dùng để lưu thời điểm bắt đầu cho chế độ HoldRepeat")]
        [Networked] public int StartTick { get; set; }
        [Networked] public float RepeatCarry { get; set; }

        [Tooltip("Dùng để lưu Tick Target làm bộ hẹn giờ trong chế độ HoldRepeat")]
        [Networked] public int NextRepeatTick { get; set; }

        [Tooltip("Lưu tổng thời gian của một lần interaction")]
        [Networked] public float ActiveDuration { get; set; }

        [Tooltip("Lưu vị trí bắt đầu tương tác. Dùng để ghi nhận sự di chuyển trong khi đang tương tác")]
        [Networked] public Vector3 StartPosition { get; set; }

        [Tooltip("Dùng để thay đổi khi Perform. ")]
        [Networked] public int PerformCounter { get; set; }

        [Networked]
        public NetworkButtons PrevButtons { get; set; }


        private InteractionAction _activeActionCache;     

        private void EndTargetLost()
        {
            var action = _activeActionCache;
            if (action != null)
            {
                // target có thể đã bị Despawn (Unity object == null), nên context phải chịu được Target null
                var ctx = new InteractionContext
                {
                    Interactor = this,
                    Target = null,
                    ItemHold = CurrentTool,
                    Elapsed = Elapsed(),
                    Progress01 = 0f
                };
                action.OnEnd(ctx, InteractionEndReason.TargetLost);
            }
            _activeActionCache = null;
            State = InteractionState.Idle;
        }


        // ---------- Events cho UI / Animator (client-side) ----------
        public event Action<InteractionEvent> Presented;
        public event Action<int, Interactable, InteractionAction> FocusChanged; // chỉ bắn trên máy local

        private IItemHoldProvider _tools;
        [Tooltip("Biến lưu trạng thái cuối cùng của State. Khi State thay đổi sẽ kích hoạt RaiseEvent trong Render")]
        private InteractionState _seenState; // 

        [Tooltip("Dùng để lưu giá trị cuối cùng trước khi thay đổi PerformCount để phát hiện và kích hoạt event Raise")]
        private int _seenPerform;
        [SerializeField, Min(0.02f)] private float _focusInterval = 0.1f;

        private NetworkId _lastTargetId;
        private InteractionState _lastFocusState;
        private float _nextFocusTime;

        /// Gọi khi dữ liệu ảnh hưởng điều kiện đổi (nhặt/mất item, đổi tool, shop mở/đóng...).
        public void RequestFocusRefresh() => _nextFocusTime = 0f;


        private Interactable[] _focusTarget;
        private InteractionAction[] _focusAction;

        private Vector3 Origin => _origin != null ? _origin.position : transform.position;
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
       

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
                for (int s = 0; s < _focusAction.Length; s++)
                    if (_focusAction[s] != null) SetFocus(s, null, null);
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

                if (Resolve(s, out var target, out var action, out var fail))
                {
                    Begin(target, action, s);
                    return;
                }

                if (!fail.IsEmpty) SendFailReason(fail);
            }
        }

        private void Begin(Interactable target, InteractionAction action, int slot)
        {
            ActiveTarget = target.Object.Id;
            ActiveActionIndex = action.Index;
            _activeActionCache = action;
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
                    RepeatCarry = 0f;
                    NextRepeatTick = StartTick + ToTicks(ActiveDuration);
                    break;
                case InteractionMode.Session:
                    State = InteractionState.Session;
                    break;
            }
        }

        private void TickHolding(NetworkButtons buttons, bool cancel)
        {
            if (!TryGetActive(out var target, out var action)) { EndTargetLost(); return; }

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

            if (!ValidateOngoing(target, action, ctx, out var failReason, out var fail))
            {
                End(action, ctx, failReason);
                if (!fail.IsEmpty) SendFailReason(fail);
                return;
            }

            action.OnServerProgress(ctx);

            if (action.Mode == InteractionMode.Hold && elapsed >= ActiveDuration)
            {
                Perform(action, ctx);
                End(action, ctx, InteractionEndReason.Completed);
            }
            else if (action.Mode == InteractionMode.HoldRepeat && Runner.Tick.Raw >= NextRepeatTick)
            {
                float t = action.GetRepeatInterval(ctx) / Runner.DeltaTime + RepeatCarry;
                int ticks = Mathf.Max(1, Mathf.FloorToInt(t));
                RepeatCarry = t - ticks;
                NextRepeatTick += ticks;
                Perform(action, ctx);
            }
        }

        private void TickSession(NetworkButtons pressed, bool cancel)
        {
            if (!TryGetActive(out var target, out var action)) { EndTargetLost(); return; }

            var ctx = MakeContext(target, Elapsed(), 0f);

            if (cancel || pressed.IsSet(_slotBits[ActiveSlot])) { End(action, ctx, InteractionEndReason.Cancelled); return; }
            if (!ValidateOngoing(target, action, ctx, out var failReason, out var fail))
            {
                End(action, ctx, failReason);
                if (!fail.IsEmpty) SendFailReason(fail);
                return;
            }
            if (!action.OnSessionTick(ctx)) End(action, ctx, InteractionEndReason.Completed);
        }

        private bool ValidateOngoing(Interactable target, InteractionAction action, in InteractionContext ctx, out InteractionEndReason reason, out InteractionFail fail)
        {
            reason = InteractionEndReason.Completed;
            fail = default;
            if (FlatDistance(Origin, target.FocusPoint.position) > action.EffectiveRange + _rangeLeeway)
            { reason = InteractionEndReason.OutOfRange; return false; }

            if (action.CancelOnMove && action.MoveSpeedMultiplier > 0f)
            {
                var d = transform.position - StartPosition; d.y = 0f;
                if (d.sqrMagnitude > action.MoveTolerance * action.MoveTolerance)
                { reason = InteractionEndReason.Moved; return false; }
            }

            if (!action.CanInteract(ctx, out fail)) { reason = InteractionEndReason.ConditionFailed; return false; }
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
        /// 
        public bool Resolve(int slot, out Interactable target, out InteractionAction action)
     => Resolve(slot, out target, out action, out _);

        public bool Resolve(int slot, out Interactable target, out InteractionAction action, out InteractionFail fail)
        {
            target = null; action = null; fail = default;

            if (suggestTarget == null) return false;

            var id = suggestTarget.CurrentTargetId;
            if (!id.IsValid) return false;

            var no = Runner.FindObject(id);
            if (no == null || !no.IsValid) return false;
            if (!_allowSelf && no == Object) return false;

            var it = no.GetComponentInChildren<Interactable>();
            if (it == null) return false;

            var to = it.FocusPoint.position - Origin; to.y = 0f;
            var ctx = MakeContext(it, 0f, 0f);

            if (!it.TryGetBestAction(slot, ctx, to.magnitude, out var a, out fail)) return false;

            target = it;
            action = a;
            return true;
        }

        // ================= Presentation (mọi client) =================

        public override void Render()
        {
            var st = State;
            if (_seenState == InteractionState.Idle && st != InteractionState.Idle)
            {
                Raise(InteractionEventType.Begin);
                modelPlayer ??= transform.GetChild(0).gameObject;
                if (Runner.TryFindObject(ActiveTarget, out NetworkObject target))
                {
                    var dir = (target.transform.position - transform.position).normalized;
                    modelPlayer.transform.localRotation = Quaternion.LookRotation(dir);
                }
            }
            if (PerformCounter != _seenPerform) { _seenPerform = PerformCounter; Raise(InteractionEventType.Perform); }
            if (_seenState != InteractionState.Idle && st == InteractionState.Idle) Raise(InteractionEventType.End);
            _seenState = st;

            if (st == InteractionState.Holding && TryGetActive(out var t, out var a))
            {
                float e = Elapsed();
                var ctx = MakeContext(t, e, ActiveDuration > 0f ? Mathf.Clamp01(e / ActiveDuration) : 0f);
                a.OnProgress(ctx, Object.HasInputAuthority);
            }

            if (Object.HasInputAuthority) UpdateFocus();
        }

        private void Raise(InteractionEventType type)
        {
            bool alive = TryGetActive(out var t, out var a);
            if (!alive) 
            { 
             if (type != InteractionEventType.End) return;   // End vẫn phải đi tiếp
            }
            float e = Elapsed();
            var ctx = MakeContext(t, e, ActiveDuration > 0f ? Mathf.Clamp01(e / ActiveDuration) : 0f);
            bool local = Object.HasInputAuthority;

            if (alive) a.OnPresentation(type, ctx, local);   // Action chết thì bỏ qua hook của nó
            Presented?.Invoke(new InteractionEvent(type, ctx, alive ? a : null, local));
        }

        private void UpdateFocus()
        {
            var id = suggestTarget != null ? suggestTarget.CurrentTargetId : default;
            bool changed = !id.Equals(_lastTargetId) || State != _lastFocusState;

            if (!changed && Time.unscaledTime < _nextFocusTime) return;

            _lastTargetId = id;
            _lastFocusState = State;
            _nextFocusTime = Time.unscaledTime + _focusInterval;

            for (int s = 0; s < _slotBits.Length; s++)
            {
                Interactable t = null; InteractionAction a = null;
                if (State == InteractionState.Idle) Resolve(s, out t, out a);

                if (t != _focusTarget[s] || a != _focusAction[s])
                    SetFocus(s, t, a);
                else if (a != null)
                    a.OnFocusRefresh(MakeContext(t, 0f, 0f));   // vẫn focus: cập nhật chữ prompt
            }
        }

        private void SetFocus(int slot, Interactable target, InteractionAction action)
        {
            var oldTarget = _focusTarget[slot];
            var oldAction = _focusAction[slot];

            // oldAction == null cũng đúng khi object cũ đã bị destroy (Unity null) -> bỏ qua an toàn
            if (oldAction != null) oldAction.OnFocusExit(MakeContext(oldTarget, 0f, 0f));

            _focusTarget[slot] = target;
            _focusAction[slot] = action;

            if (action != null) action.OnFocusEnter(MakeContext(target, 0f, 0f));

            FocusChanged?.Invoke(slot, target, action);
        }

        // ================= Helpers =================


        void SendFailReason(InteractionFail fail)
        {
            if (!HasStateAuthority) return;
            RPC_ShowNotification(fail.Message, fail.Code);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        public void RPC_ShowNotification(string message, int code)
        {
            NotificationManager.Ins.ShowNotification(message, code);
        }

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
            Interactor = this,
            Target = target,
            ItemHold = CurrentTool,
            Elapsed = elapsed,
            Progress01 = progress,
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
