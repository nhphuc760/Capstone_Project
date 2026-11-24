using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Đạn pháo bay theo đường cong (parabol) từ nòng súng tới đúng vị trí target.
    ///
    /// Prefab đạn cần: NetworkObject + NetworkTransform + CannonShell (+ model/trail tuỳ ý).
    /// Server (StateAuthority) tự tính vị trí theo tick trong FixedUpdateNetwork, NetworkTransform lo
    /// replicate + nội suy cho client => mọi máy thấy cùng một quỹ đạo, client không gian lận được.
    ///
    /// Quỹ đạo: p(t) = Lerp(Start, Target, t) + Up * 4 * ArcHeight * t * (1 - t),  t = 0..1
    ///   - t = 0: đúng vị trí nòng súng
    ///   - t = 1: đúng vị trí target (ép vị trí = Target rồi mới nổ)
    ///   - t = 0.5: đỉnh cung, cao hơn đường thẳng đúng ArcHeight
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public class CannonShell : NetworkBehaviour
    {
        [Header("Flight")]
        [Tooltip("Tốc độ bay theo phương ngang (m/s). Thời gian bay = khoảng cách / tốc độ")]
        [SerializeField, Min(1f)] private float _speed = 14f;
        [SerializeField, Min(0.1f)] private float _minFlightTime = 0.6f;
        [SerializeField, Min(0.2f)] private float _maxFlightTime = 3f;

        [Header("Arc")]
        [Tooltip("Độ cao đỉnh cung = khoảng cách * hệ số này (rồi kẹp trong min/max)")]
        [SerializeField, Min(0f)] private float _arcHeightPerMeter = 0.35f;
        [SerializeField, Min(0f)] private float _minArcHeight = 2f;
        [SerializeField, Min(0f)] private float _maxArcHeight = 10f;

        [Header("Impact")]
        [SerializeField, Min(0f)] private float _explosionRadius = 3f;
        [SerializeField] private LayerMask _hitLayers = ~0;
        [Tooltip("Tuỳ chọn: prefab VFX nổ thường (KHÔNG phải NetworkObject), sinh ở mọi client khi đạn despawn")]
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField, Min(0f)] private float _impactVfxLifetime = 3f;

        // ---------- Networked ----------
        [Networked] public Vector3 StartPos { get; set; }
        [Networked] public Vector3 TargetPos { get; set; }
        [Networked] public int StartTick { get; set; }
        [Networked] public float FlightTime { get; set; }
        [Networked] public float ArcHeight { get; set; }
        [Networked] public PlayerRef Shooter { get; set; }

        private bool _landed;
        private readonly HashSet<NetworkObject> _hitObjects = new HashSet<NetworkObject>();


        float _damage;

        /// <summary>
        /// Gọi trong onBeforeSpawned của Runner.Spawn (trước khi đạn xuất hiện trên mạng).
        /// start = vị trí nòng súng, target = vị trí mục tiêu đã được server kiểm tra.
        /// </summary>
        public void Init(Vector3 start, Vector3 target, PlayerRef shooter, float damage)
        {
            StartPos = start;
            TargetPos = target;
            Shooter = shooter;
            StartTick = Runner != null ? Runner.Tick.Raw : 0;
            _damage = damage;   
            var flat = target - start; flat.y = 0f;
            float dist = flat.magnitude;

            FlightTime = Mathf.Clamp(dist / Mathf.Max(0.01f, _speed), _minFlightTime, _maxFlightTime);
            ArcHeight = Mathf.Clamp(dist * _arcHeightPerMeter, _minArcHeight, _maxArcHeight);
        }

        public override void Spawned()
        {
            transform.position = StartPos;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || _landed) return;

            float t = FlightTime > 0f
                ? Mathf.Clamp01((Runner.Tick.Raw - StartTick) * Runner.DeltaTime / FlightTime)
                : 1f;

            if (t >= 1f)
            {
                transform.position = TargetPos;   // đáp đúng vị trí mục tiêu, không lệch do làm tròn tick
                _landed = true;
                OnImpact(TargetPos);
                Runner.Despawn(Object);
                return;
            }

            var pos = Evaluate(t);
            // Quay đầu đạn theo hướng bay (đạo hàm xấp xỉ bằng điểm kế tiếp)
            var next = Evaluate(Mathf.Min(1f, t + 0.02f));
            var dir = next - pos;
            var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : transform.rotation;

            transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Điểm trên cung parabol tại t (0..1).</summary>
        private Vector3 Evaluate(float t)
        {
            var p = Vector3.Lerp(StartPos, TargetPos, t);
            p.y += 4f * ArcHeight * t * (1f - t);
            return p;
        }

        /// <summary>
        /// HOOK NỔ: chạy trên server khi đạn chạm đích. Gây sát thương vùng ở đây.
        /// Override hoặc sửa trực tiếp.
        /// </summary>
        protected virtual void OnImpact(Vector3 point)
        {
            Debug.Log($"[Shell] NỔ tại {point} (bán kính {_explosionRadius}m, người bắn={Shooter})", this);

            _hitObjects.Clear();
            var cols = Physics.OverlapSphere(point, _explosionRadius, _hitLayers);

            //foreach (var i in cols) 
            //{
            //    if(i.transform.root.TryGetComponent<NetworkObject>(out var obj))
            //    {
            //        if (obj.HasInputAuthority) continue;
            //        if(obj.TryGetComponent<ITakedamageable>(out var takedamageable))
            //        {
            //            takedamageable.TakeDamage(_damage, Object);
            //        }
            //    }
            //}



            for (int i = 0; i < cols.Length; i++)
            {
                var no = cols[i].GetComponentInParent<NetworkObject>();
                if (no == null || no.InputAuthority == Object.InputAuthority || !_hitObjects.Add(no)) continue;
                if(no.TryGetComponent<ITakedamageable>(out var takedamageable))
                {
                    takedamageable.TakeDamage(_damage, Object);
                }
                // TODO: gây sát thương / đẩy lùi / phá cửa... cho 'no' ở đây
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Chạy trên mọi peer: dựng hiệu ứng nổ tại đúng vị trí đích
            if (!hasState || _impactVfxPrefab == null) return;

            var vfx = Instantiate(_impactVfxPrefab, TargetPos, Quaternion.identity);

            if (_impactVfxLifetime > 0f) Destroy(vfx, _impactVfxLifetime);
        }
    }
}
