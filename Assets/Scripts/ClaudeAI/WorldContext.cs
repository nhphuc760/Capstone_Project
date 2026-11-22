using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Thông tin world dùng CHUNG cho mọi quái: danh sách player/cửa, ngày đêm, vùng thành,
    /// lãnh địa hồi máu, và "claim" (bao nhiêu quái đang nhắm mỗi mục tiêu) để cân bằng.
    /// Chỉ cần chạy chính xác trên host (nơi AI chạy).
    /// </summary>
    public class WorldContext : NetworkBehaviour
    {
        public static WorldContext Instance { get; private set; }

        [Header("Ngày/Đêm (kéo component implement IDayNightProvider vào)")]
        [SerializeField] MonoBehaviour dayNightSource;

        [Header("Kiểm tra Inside EnclosedZone")]
        [SerializeField] StructureManager structureManager;
        [SerializeField] GameManager gameManager;
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
            Debug.Log("Get Territory ");
            territoryPoints = mapManager.ZoneGroupsByType[ZoneType.Red];
            _path = new NavMeshPath();
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
        }

        // ---------- Vùng thành ----------

        /// <summary>
        /// Kiểm tra Player có đang bên trong nhà hay ở ngoài
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>

        public bool IsInsideCastle(PlayerRef player, Vector3 p)
        {
            return structureManager.IsInsideEnclosedZone(player, p);
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
            if(NavMesh.SamplePosition(territoryPoints[i].center, out var hit, 2f, NavMesh.AllAreas))
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
            int n = territoryPoints.Count;
            if (n == 0) return false;
            EnsureOccupancy();

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
                _cand.Add(best);
            }

            float bestScore = float.MaxValue, bestLen = 0f;
            for (int k = 0; k < _cand.Count; k++)
            {
                int i = _cand[k];
                Vector3 pos = territoryPoints[i].center;
                float len = PathLength(from, pos);
                float score = len + _occupancy[i] * occupancyPenalty + CountThreat(pos) * threatPenalty;
                if (score < bestScore) { bestScore = score; index = i; bestLen = len; }
            }

            travelSeconds = bestLen / Mathf.Max(0.1f, speed);
            return index >= 0;
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
