using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Thông tin world dùng CHUNG cho mọi quái: danh sách player/cửa, ngày đêm, vùng thành,
    /// lãnh địa hồi máu, "claim" (bao nhiêu quái đang nhắm mỗi mục tiêu) và trạng thái "lộ diện"
    /// của từng player (có trễ hai chiều để chống ra-vào liên tục).
    /// </summary>
    public class WorldContext : NetworkBehaviour
    {
        public static WorldContext Instance { get; private set; }

        [Header("Ngày/Đêm (kéo component implement IDayNightProvider vào)")]
        [SerializeField] MonoBehaviour dayNightSource;

        [Header("Kiểm tra Inside EnclosedZone")]
        [SerializeField] StructureManager structureManager;
        [SerializeField] GameManager gameManager;

        [Header("Lộ diện / ẩn nấp (chống player ra-vào thành để lật mục tiêu)")]
        [SerializeField] float exposeDelay = 0.6f;     // ra ngoài liên tục >= giá trị này mới bị coi là "lộ diện"
        [SerializeField] float hideDelay = 3f;         // ở trong thành liên tục >= giá trị này mới được coi là "đã ẩn"
        [SerializeField] float exposureTickSeconds = 0.2f;

        [Header("Lãnh địa hồi máu (danh sách có sẵn của bạn)")]
        [SerializeField] MapManager mapManager;
        [SerializeField] List<MapManager.ZoneGroup> territoryPoints = new List<MapManager.ZoneGroup>();
        [SerializeField] int pathCandidates = 3;       // số điểm gần nhất được tính đường đi NavMesh thật
        [SerializeField] float occupancyPenalty = 12f; // phạt mỗi quái đã đặt chỗ ở điểm đó
        [SerializeField] float threatRadius = 12f;
        [SerializeField] float threatPenalty = 15f;    // phạt mỗi player sống đứng gần điểm đó

        public IDayNightProvider DayNight { get; set; }

        readonly List<IMonsterTarget> _players = new List<IMonsterTarget>();
        readonly List<IMonsterTarget> _gates = new List<IMonsterTarget>();
        readonly Dictionary<NetworkId, int> _claims = new Dictionary<NetworkId, int>();
        int[] _occupancy;
        readonly List<int> _cand = new List<int>(8);
        static NavMeshPath _path;

        class Exposure
        {
            public bool exposed;     // đã qua bộ lọc trễ -> quái coi là "ở ngoài"
            public bool insideNow;   // vị trí thật tại lần quét gần nhất
            public float outTimer, inTimer;
        }
        readonly Dictionary<NetworkId, Exposure> _exposure = new Dictionary<NetworkId, Exposure>();
        float _exposureAcc;

        public IReadOnlyList<IMonsterTarget> Players => _players;
        public IReadOnlyList<IMonsterTarget> Gates => _gates;

        private void Awake()
        {
            Instance = this;
        }

        public override async void Spawned()
        {
            if (DayNight == null) DayNight = dayNightSource as IDayNightProvider;

            await UniTask.WaitUntil(() => gameManager.Object.IsValid && gameManager.CurrentState == GameState.Playing);
            territoryPoints = mapManager.ZoneGroupsByType[ZoneType.Red];
            _path = new NavMeshPath();
            Debug.Log($"[WorldContext] Đã nạp lãnh địa: {territoryPoints.Count} điểm (ZoneType.Red) lúc state={gameManager.CurrentState}");
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // ---------- Ngày/đêm ----------
        public bool IsNight => DayNight != null && DayNight.IsNight;
        public float SecondsUntilDawn => DayNight != null ? DayNight.SecondsUntilDawn : 0f;

        // ---------- Đăng ký mục tiêu ----------
        public void Register(IMonsterTarget t)
        {
            var list = t.Kind == TargetKind.Gate ? _gates : _players;
            if (!list.Contains(t)) list.Add(t);
        }

        public void Unregister(IMonsterTarget t)
        {
            (t.Kind == TargetKind.Gate ? _gates : _players).Remove(t);
            _claims.Remove(t.TargetId);
            _exposure.Remove(t.TargetId);
        }

        // ---------- Vùng thành & lộ diện ----------

        /// <summary>Kiểm tra thô: vị trí p có nằm trong vùng thành khép kín của player không.</summary>
        public bool IsInsideCastle(PlayerRef player, Vector3 p)
        {
            return structureManager.IsInsideEnclosedZone(player, p);
        }

        /// <summary>
        /// Player có bị coi là "đang ở ngoài" không (đã qua bộ lọc trễ hai chiều).
        /// Ra ngoài >= exposeDelay mới lộ; vào trong >= hideDelay mới ẩn.
        /// </summary>
        public bool IsExposed(NetworkId id) => _exposure.TryGetValue(id, out var e) && e.exposed;

        /// <summary>Vị trí thật (không trễ) của player: đang nằm trong thành?</summary>
        public bool IsInsideNow(NetworkId id) => _exposure.TryGetValue(id, out var e) && e.insideNow;

        public bool AnyGateAlive
        {
            get
            {
                for (int i = 0; i < _gates.Count; i++) if (_gates[i].IsTargetable) return true;
                return false;
            }
        }

        public override void FixedUpdateNetwork()
        {
            _exposureAcc += Runner.DeltaTime;
            if (_exposureAcc < exposureTickSeconds) return;
            float dt = _exposureAcc;
            _exposureAcc = 0f;

            for (int i = 0; i < _players.Count; i++)
            {
                var t = _players[i];
                if (!_exposure.TryGetValue(t.TargetId, out var e))
                {
                    e = new Exposure { insideNow = true };
                    _exposure[t.TargetId] = e;
                }

                var no = Runner.FindObject(t.TargetId);
                bool inside = no == null || IsInsideCastle(no.InputAuthority, t.Position);
                e.insideNow = inside;

                if (!e.exposed)
                {
                    e.outTimer = inside ? 0f : e.outTimer + dt;
                    if (e.outTimer >= exposeDelay) { e.exposed = true; e.inTimer = 0f; }
                }
                else
                {
                    e.inTimer = inside ? e.inTimer + dt : 0f;
                    if (e.inTimer >= hideDelay) { e.exposed = false; e.outTimer = 0f; }
                }
            }
        }

        // ---------- Claim (cân bằng mục tiêu) ----------
        public int GetClaims(NetworkId targetId) => _claims.TryGetValue(targetId, out var c) ? c : 0;
        public void AddClaim(NetworkId targetId) => _claims[targetId] = GetClaims(targetId) + 1;
        public void RemoveClaim(NetworkId targetId)
        {
            int c = GetClaims(targetId) - 1;
            if (c <= 0) _claims.Remove(targetId); else _claims[targetId] = c;
        }

        // ---------- Lãnh địa ----------
        public int TerritoryCount => territoryPoints.Count;
        public Vector3 TerritoryPosition(int i)
        {
            if (NavMesh.SamplePosition(territoryPoints[i].center, out var hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return territoryPoints[i].center;
        }

        void EnsureOccupancy()
        {
            if (_occupancy == null || _occupancy.Length != territoryPoints.Count)
                _occupancy = new int[territoryPoints.Count];
        }

        public void ReserveTerritory(int i) { EnsureOccupancy(); _occupancy[i]++; }
        public void ReleaseTerritory(int i) { EnsureOccupancy(); _occupancy[i] = Mathf.Max(0, _occupancy[i] - 1); }

        /// <summary>
        /// Chọn lãnh địa tốt nhất: gần theo đường đi NavMesh thật, ít quái đặt chỗ, ít player đứng canh.
        /// </summary>
        public bool PickTerritory(Vector3 from, float speed, out int index, out float travelSeconds)
        {
            index = -1; travelSeconds = 0f;
            int n = territoryPoints != null ? territoryPoints.Count : 0;

            // ---- DEBUG: lý do thất bại số 1 - danh sách rỗng (thường do gọi trước khi Spawned() nạp xong) ----
            if (n == 0)
            {
                DbgFail($"territoryPoints RỖNG (null={territoryPoints == null}, mapManager null={mapManager == null}, " +
                        $"gameState={(gameManager != null ? gameManager.CurrentState.ToString() : "gameManager null")}). " +
                        "Nếu quái spawn trước khi WorldContext.Spawned() chạy xong phần await thì list chưa được nạp.");
                return false;
            }
            EnsureOccupancy();

            if (float.IsNaN(from.x) || float.IsNaN(from.y) || float.IsNaN(from.z))
            {
                DbgFail($"vị trí quái bị NaN: {from}");
                return false;
            }

            _cand.Clear();
            int take = Mathf.Min(pathCandidates, n);
            for (int k = 0; k < take; k++)
            {
                int best = -1; float bd = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (_cand.Contains(i)) continue;
                    float d = (territoryPoints[i].center - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = i; }
                }
                // ---- DEBUG: best == -1 xảy ra khi center là NaN/Infinity (so sánh luôn false) ----
                if (best < 0)
                {
                    DbgFail($"không chọn được ứng viên thứ {k}: có territoryPoints[].center bị NaN/Infinity? " +
                            $"(n={n}, take={take}, đã chọn={_cand.Count})");
                    continue;
                }
                _cand.Add(best);
            }

            float bestScore = float.MaxValue, bestLen = 0f;
            var sb = debugPickTerritory ? new System.Text.StringBuilder() : null;
            sb?.AppendLine($"[PickTerritory] from={from} speed={speed:F2} n={n} candidates={_cand.Count} navmeshAgent-from-OnNavMesh={NavMesh.SamplePosition(from, out _, 2f, NavMesh.AllAreas)}");

            for (int k = 0; k < _cand.Count; k++)
            {
                int i = _cand[k];
                Vector3 pos = territoryPoints[i].center;
                float len = PathLength(from, pos);
                int threat = CountThreat(pos);
                float score = len + _occupancy[i] * occupancyPenalty + threat * threatPenalty;

                bool nan = float.IsNaN(score) || float.IsInfinity(score);
                sb?.AppendLine($"   #{i} center={pos} straight={Vector3.Distance(from, pos):F1} pathLen={len:F1} " +
                               $"occ={_occupancy[i]} threat={threat} score={score:F1}{(nan ? "  <-- NaN/Inf, BỊ BỎ QUA" : "")}");

                // ---- DEBUG: score NaN thì `score < bestScore` luôn false => index kẹt -1 => trả về false ----
                if (nan) continue;

                if (score < bestScore) { bestScore = score; index = i; bestLen = len; }
            }

            travelSeconds = bestLen / Mathf.Max(0.1f, speed);

            if (index < 0)
            {
                DbgFail("duyệt xong mọi ứng viên mà không có điểm nào hợp lệ (xem log chi tiết bên dưới)\n" + sb);
                return false;
            }

            if (sb != null)
            {
                sb.AppendLine($"   => CHỌN #{index} travel={travelSeconds:F1}s");
                Debug.Log(sb.ToString());
            }
            return true;
        }

        // =====================================================================
        //  DEBUG cho PickTerritory
        // =====================================================================
        [Header("Debug")]
        [SerializeField] bool debugPickTerritory = true;     // log chi tiết CẢ khi thành công
        [SerializeField] float debugFailLogInterval = 2f;    // chống spam khi thất bại (mỗi quái gọi liên tục)
        float _lastFailLog = -999f;

        void DbgFail(string reason)
        {
            if (Time.unscaledTime - _lastFailLog < debugFailLogInterval) return;
            _lastFailLog = Time.unscaledTime;
            Debug.LogWarning("[PickTerritory] THẤT BẠI: " + reason, this);
        }

        /// <summary>Chuột phải vào component WorldContext trong Inspector (lúc đang Play) để xem toàn bộ lãnh địa.</summary>
        [ContextMenu("Debug/Dump Territory")]
        void DumpTerritory()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[Territory] count={territoryPoints?.Count ?? -1}, mapManager null={mapManager == null}, " +
                          $"pathCandidates={pathCandidates}, occupancy.Length={(_occupancy == null ? -1 : _occupancy.Length)}");
            if (territoryPoints != null)
            {
                for (int i = 0; i < territoryPoints.Count; i++)
                {
                    Vector3 c = territoryPoints[i].center;
                    bool onMesh = NavMesh.SamplePosition(c, out var hit, 2f, NavMesh.AllAreas);
                    sb.AppendLine($"   #{i} center={c} onNavMesh(2m)={onMesh}" + (onMesh ? $" snapped={hit.position}" : ""));
                }
            }
            Debug.Log(sb.ToString(), this);
        }

        public float EstimateTravelSeconds(Vector3 from, Vector3 to, float speed) =>
            PathLength(from, to) / Mathf.Max(0.1f, speed);

        int CountThreat(Vector3 pos)
        {
            int c = 0;
            for (int i = 0; i < _players.Count; i++)
                if (_players[i].IsTargetable && (_players[i].Position - pos).sqrMagnitude < threatRadius * threatRadius) c++;
            return c;
        }

        public static float PathLength(Vector3 a, Vector3 b)
        {
            if (_path == null) _path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, _path) ||
                _path.status == NavMeshPathStatus.PathInvalid)
                return Vector3.Distance(a, b) * 2f; // không có đường: ước lượng thận trọng

            var c = _path.corners;
            float len = 0f;
            for (int i = 1; i < c.Length; i++) len += Vector3.Distance(c[i - 1], c[i]);
            return len;
        }
    }
}
