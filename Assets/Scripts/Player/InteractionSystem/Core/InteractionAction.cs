using Fusion;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Đơn vị hành vi. Muốn thêm kiểu tương tác mới: kế thừa class này, override hook cần dùng,
    /// gắn vào prefab cạnh Interactable. KHÔNG cần sửa Interactable / InteractionController.
    ///
    /// Hook "logic" (OnBegin/OnProgress/OnPerform/OnEnd/OnSessionTick/OnPayload) chỉ chạy trên StateAuthority.
    /// Hook "OnCanInteract / GetHoldDuration / GetPrompt" chạy ở mọi nơi (server + client để hiện prompt).
    /// Hook "OnPresentation" chạy trên MỌI client (animation, vfx, sfx, mở UI nếu isLocal).
    /// Là NetworkBehaviour nên có thể có [Networked] state riêng (cooldown, durability...).
    /// </summary>
    public abstract class InteractionAction : NetworkBehaviour
    {
        [Header("Binding")]
        [SerializeField, Tooltip("là Input mà nó phản ứng")] private int _slot = 0;           // 0 = nút chính, 1 = nút phụ
        [Tooltip("cao hơn được xét trước nếu có nhiều hành động trên cùng đối tượng")]
        [SerializeField] private int _priority = 0;       // cao hơn được xét trước
        [Tooltip("Là trạng thái mà nó phản ứng. Press/Hold/Release")]
        [SerializeField] private InteractionMode _mode = InteractionMode.Press;

        [Header("Timing")]
        [Tooltip("Thời gian giữ nút")]
        [SerializeField, Min(0f)] private float _holdDuration = 1f;    // Hold: thời gian giữ; HoldRepeat: windup; Release: thời gian nạp tối đa
        [Tooltip("Khởi thời gian lặp trong chế độ HoldRepeat")]
        [SerializeField, Min(0.05f)] private float _repeatInterval = 0.6f;
        [Tooltip("Thời gian giữ tối tiểu mới bắt đầu tính Hold")]
        [SerializeField, Min(0f)] private float _minHoldTime = 0.1f;   // Release: giữ tối thiểu mới tính

        [Header("Range / Movement")]
        [Tooltip("Mặc định là -1 nên sẽ không sử dụng, thay vào đó là sử dụng defaultRange của Interactable. Nếu giá trị này lớn hơn 0 thì nó sẽ được sử dụng ghi đè thay cho defaultRange")]
        [SerializeField] private float _maxRange = -1f;                // <=0: dùng DefaultRange của Interactable
        [Tooltip(" 0 = khoá di chuyển khi đang tương tác")]
        [SerializeField, Range(0f, 1f)] private float _moveSpeedMultiplier = 1f; // 0 = khoá di chuyển khi đang tương tác

        [Tooltip("Hủy tương tác khi di chuyển")]
        [SerializeField] private bool _cancelOnMove = false;

        [Tooltip("Ngưỡng xác định là di chuyển, tăng cao nếu hiện tượng giật lắc gây hủy trạng thái ")]
        [SerializeField, Min(0.05f)] private float _moveTolerance = 0.3f;

        [Header("Rules & Presentation")]
        [SerializeField] private InteractionCondition[] _conditions;

        [SerializeField] private InteractionPresentation _presentation = new InteractionPresentation();

        public int Slot => _slot;
        public int Priority => _priority;
        public InteractionMode Mode => _mode;
        public float HoldDuration => _holdDuration;
        public float RepeatInterval => _repeatInterval;
        public float MinHoldTime => _minHoldTime;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
        public bool CancelOnMove => _cancelOnMove;
        public float MoveTolerance => _moveTolerance;
        public InteractionPresentation Presentation => _presentation;

        
        public Interactable Owner { get; private set; }
        public byte Index { get; private set; }

        /// <summary>
        /// Hàm khởi tạo, ràng buộc action với Interactable
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="index"></param>
        public void Bind(Interactable owner, byte index) { Owner = owner; Index = index; }

        public float EffectiveRange => _maxRange > 0f ? _maxRange : (Owner != null ? Owner.DefaultRange : 2.5f); //Phạm vi tương tác của đối tượng

        /// <summary>
        /// So sánh với EffectiveRange(Phạm vi tương tác của đối tượng)
        /// </summary>
        /// <param name="flatDistance"></param>
        /// <returns></returns>
        public bool IsInRange(float flatDistance) => flatDistance <= EffectiveRange;

        /// <summary>Kiểm tra Condition (Inspector) rồi tới logic riêng.</summary>
        public bool CanInteract(in InteractionContext ctx)
        {
            if (!isActiveAndEnabled) return false;
            if (_conditions != null)
                for (int i = 0; i < _conditions.Length; i++)
                    if (_conditions[i] != null && !_conditions[i].Evaluate(ctx)) return false;
            return OnCanInteract(ctx);
        }

        // ---------- Hook: mọi peer ----------
        public virtual bool OnCanInteract(in InteractionContext ctx) => true;
        public virtual float GetHoldDuration(in InteractionContext ctx) => _holdDuration;
        public virtual float GetRepeatInterval(in InteractionContext ctx) => _repeatInterval;
        public virtual string GetPrompt(in InteractionContext ctx) => _presentation.Prompt;

        public virtual float GetAnimationSpeed(in InteractionContext ctx) => 1f;
        /// <summary>Animation/VFX/SFX/UI. isLocal = máy của người tương tác (dùng để mở UI radar/pháo).</summary>
        public virtual void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal) { }

        // ---------- Hook: chỉ StateAuthority ----------
        public virtual void OnBegin(in InteractionContext ctx) { }
        public virtual void OnProgress(in InteractionContext ctx) { }
        public virtual void OnPerform(in InteractionContext ctx) { }
        public virtual void OnEnd(in InteractionContext ctx, InteractionEndReason reason) { }
        /// <summary>Session: trả false để tự kết thúc phiên.</summary>
        public virtual bool OnSessionTick(in InteractionContext ctx) => true;
        /// <summary>Nhận dữ liệu client gửi lên qua InteractionController.SendPayload (đã được kiểm tra đang active).</summary>
        public virtual void OnPayload(in InteractionContext ctx, in InteractionPayload payload) { }
    }
}
