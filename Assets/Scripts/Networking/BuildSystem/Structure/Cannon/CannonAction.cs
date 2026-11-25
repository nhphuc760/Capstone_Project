using Fusion;
using TMPro;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Súng pháo (Session). Gắn cạnh Interactable trên prefab Cannon (cần NetworkObject).
    /// BẮT BUỘC chỉnh trong Inspector: Mode = Session, Slot = 0 (nút Interact).
    /// - Interact lần 1: mở phiên -> máy local hiện vòng range + tâm ngắm (CannonAimView).
    /// - Click chuột: client gửi toạ độ lên server qua SendPayload -> OnPayload kiểm tra
    ///   (range, cooldown, đạn) rồi bắn.
    /// - Interact lần 2: Controller tự kết thúc phiên (TickSession: bấm lại slot = Cancelled).
    /// </summary>
    public class CannonAction : InteractionAction
    {
        [Header("Cannon - Visual")]
        [SerializeField] private CannonAimView _aimView;      // chỉ dùng ở máy local
        [SerializeField] private Transform _barrel;           // nòng xoay về hướng bắn (tuỳ chọn)
        [SerializeField] private Transform _muzzle;           // vị trí đạn xuất hiện (tuỳ chọn)
        [SerializeField] private ParticleSystem _muzzleVfx;   // (tuỳ chọn)
        [SerializeField] Stats _stats;


        [Header("Cannon - Firing")]
        [SerializeField, Min(0.5f)] private float _fireRange = 12f;
        [SerializeField, Min(0f)] private float _minRange = 2f;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 2f;

        [Tooltip("Dung sai server khi kiểm tra range (tránh false-negative do lag)")]

        [SerializeField, Min(0f)] private float _rangeTolerance = 0.5f;

        [Tooltip("Tuỳ chọn: prefab đạn pháo (NetworkObject) để Runner.Spawn. Để trống = chỉ log.")]
        [SerializeField] NetworkPrefabRef _shellPrefab;
        [SerializeField] ItemSO thuocno;
        [SerializeField] int requireAmount;

        [Header("SFX")]
        [SerializeField] AudioClip fireCanon;
        [SerializeField] float volume = 1f;

        [Header("Debug")]
        [Tooltip("Chưa có inventory: bật = coi như luôn có đạn")]
        [SerializeField] private bool _debugAssumeHasAmmo = true;

        // ---------- Networked ----------
        /// <summary>Người đang dùng pháo (mỗi pháo chỉ 1 người cùng lúc).</summary>
        [Networked] public TickTimer Cooldown { get; set; }
        [SerializeField] TextMeshProUGUI coolDownText;

        public float AttackSpeed => _stats.Get(StatsType.AttackSpeed) > 0f ? _stats.Get(StatsType.AttackSpeed) : 1f/_cooldownSeconds;
        public float FireRange => _stats.Get(StatsType.Range) > 0f ? _stats.Get(StatsType.Range) : _fireRange;
        public float Damage => _stats.Get(StatsType.Damage) > 0f ? _stats.Get(StatsType.Damage) : 10f;
        public float MinRange => _minRange;
        public Vector3 Center => Owner != null ? Owner.FocusPoint.position : transform.position;

        // ================= Mọi peer =================

        public override void Spawned()
        {
            Object.AssignInputAuthority(Runner.LocalPlayer);
        }
        public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
        {
            FailReason = null;
            if (Object.IsValid && !Object.HasInputAuthority)
            {
                FailReason = "";
                return false;
            }

            return true;
        }

        public override void Render()
        {
            if (Cooldown.IsRunning)
            {
                if (coolDownText)
                    coolDownText.text = Cooldown.RemainingTime(Runner).Value.ToString("F2");
            }
            else
            {
                if (coolDownText)
                    coolDownText.text = "";
            }

        }

        /// <summary>Chạy trên MỌI client. isLocal = máy của người đang điều khiển.</summary>
        public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
        {
            if (!isLocal || _aimView == null) return;

            if (type == InteractionEventType.Begin)
            {
                _aimView.Show(ctx.Interactor, this);
                if (coolDownText)
                    coolDownText.text = "";
            }
            else if (type == InteractionEventType.End)
            {
                _aimView.Hide();
            }
        }

        // ================= Chỉ StateAuthority =================

        public override void OnBegin(in InteractionContext ctx)
        {

            Debug.Log($"[Cannon] {ctx.Interactor.name} bắt đầu điều khiển pháo", this);
        }

        public override void OnEnd(in InteractionContext ctx, InteractionEndReason reason)
        {
            Debug.Log($"[Cannon] Kết thúc phiên: {reason}", this);
        }

        public override bool OnSessionTick(in InteractionContext ctx) => true; // kết thúc bằng bấm Interact lần nữa / Cancel / ra khỏi range

        public override void OnPayload(in InteractionContext ctx, in InteractionPayload payload)
        {
            if (!Object.IsValid || Object.InputAuthority != ctx.Interactor.Object.InputAuthority) return;

            var pos = payload.Position;
            if (float.IsNaN(pos.x) || float.IsNaN(pos.z) || float.IsInfinity(pos.x) || float.IsInfinity(pos.z)) return;

            if (!Cooldown.ExpiredOrNotRunning(Runner))
            {                
                Debug.Log("[Cannon] Đang hồi chiêu", this);
                return;
            }

            var center = Center;
            var flat = pos - center; flat.y = 0f;
            float dist = flat.magnitude;
            if (dist > _fireRange + _rangeTolerance || dist < _minRange - _rangeTolerance)
            {
                Debug.Log($"[Cannon] Mục tiêu ngoài tầm bắn ({dist:F1}m, cho phép {_minRange}-{_fireRange}m)", this);
                return;
            }

            if (!TryConsumeAmmo(ctx))
            {
                RPC_Notification("Hết đạn !");
                return;
            }

            Cooldown = TickTimer.CreateFromSeconds(Runner, 1f/AttackSpeed);

            var target = pos; target.y = center.y;
            Fire(ctx, target);
            RPC_PlayFireEffect(target);
        }

        // ================= Điểm hook cho bạn =================

        /// <summary>
        /// HOOK INVENTORY: kiểm tra có đạn pháo không và trừ 1 viên. Chạy trên server.
        /// Override hoặc sửa trực tiếp, ví dụ:
        ///   var inv = ctx.Get&lt;PlayerInventory&gt;();
        ///   return inv != null &amp;&amp; inv.TryRemove(shellItemId, 1);
        /// </summary>
        protected virtual bool TryConsumeAmmo(in InteractionContext ctx)
        {
            // TODO: hook inventory ở đây
            if (ctx.Get<NetworkInventory>().RemoveItem(thuocno.ItemId, requireAmount))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// HOOK BẮN: chạy trên server sau khi đã qua mọi kiểm tra.
        /// Sinh đạn / gây sát thương vùng tại target nằm ở đây.
        /// </summary>
        protected virtual void Fire(in InteractionContext ctx, Vector3 target)
        {
            Debug.Log($"[Cannon] BẮN! target={target}", this);

            if (_shellPrefab.IsValid)
            {
                var from = _muzzle != null ? _muzzle.position : Center;
                var dir = target - from; dir.y = 0f;
                var rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

                // 'ctx' là tham số 'in' nên không dùng được trong lambda => copy ra biến cục bộ
                var shooter = ctx.Player;

                // Đạn tự bay theo cung từ 'from' (nòng súng) tới đúng 'target'; sát thương xử lý trong CannonShell.OnImpact
                Runner.Spawn(_shellPrefab, from, rot, Object.InputAuthority, (runner, obj) =>
                {
                    var shell = obj.GetComponent<CannonShell>();
                    if (shell != null) shell.Init(from, target, shooter, Damage);
                    else Debug.LogWarning("[Cannon] Prefab đạn thiếu component CannonShell", this);
                });
            }
            else
            {
                // Chưa có prefab đạn: chỉ log. Muốn thử sát thương vùng thì gọi trực tiếp tại đây.
                Debug.Log("[Cannon] Chưa gán _shellPrefab, bỏ qua spawn đạn", this);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayFireEffect(Vector3 target)
        {
            if (_barrel != null)
            {
                var d = target - _barrel.position; d.y = 0f;
                if (d.sqrMagnitude > 0.001f) _barrel.rotation = Quaternion.LookRotation(d);
            }
            if (_muzzleVfx != null) _muzzleVfx.Play();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX3D(fireCanon, transform.position, volume);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        void RPC_Notification(string message)
        {
            if(NotificationManager.Ins != null)
                NotificationManager.Ins.ShowNotification(message);
        }
    }
}
