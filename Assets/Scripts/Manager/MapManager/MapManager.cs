using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Quản lý map: noise biome, zone type, và phân tích nhóm vùng (Connected Components).
/// Không spawn resource node – phần đó thuộc ResourceManager.
/// </summary>
public class MapManager : NetworkBehaviour
{
   
    public static MapManager Ins { get; private set; }

    [Header("Map Boundaries")]
    public float mapWidth = 50f;
    public float mapHeight = 50f;

    [Header("Noise / Biome Settings")]
    [Networked]
    public float noiseScale { get; set; } = 0.04f;
    [Networked]
    public Vector2 noiseOffset { get; set; }
    [Range(0, 1)] public float yellowThreshold = 0.45f;
    [Range(0, 1)] public float redThreshold = 0.70f;

    [Header("Zone Configurations")]
    public ZoneSpawnConfig greenZoneConfig;
    public ZoneSpawnConfig yellowZoneConfig;
    public ZoneSpawnConfig redZoneConfig;


    [Header("Spawns Shop")]
    [SerializeField] LayerMask obstacleMask;





    [Header("Zone Group Settings")]
    [Tooltip("Kích thước mỗi ô khi phân tích nhóm vùng (càng nhỏ càng chính xác nhưng chậm hơn)")]
    public float zoneCellSize = 1.0f;
    public bool DrawDebug = true;
    // ===================== Runtime Zone Data =====================
    public List<ZoneGroup> AllZoneGroups { get; private set; } = new List<ZoneGroup>();

    public Dictionary<ZoneType, List<ZoneGroup>> ZoneGroupsByType { get; private set; }
        = new Dictionary<ZoneType, List<ZoneGroup>>();

    /// <summary>True sau khi GenerateMap đã chạy xong.</summary>

    private ZoneType[,] zoneGrid;
    private int gridWidth;
    private int gridHeight;

    [System.Serializable]
    public class ZoneGroup
    {
        public ZoneType type;
        public HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        public Vector3 center;
        public Bounds bounds;
        public float area;
        public int CellCount => cells.Count;
    }

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else if (Ins != this) Destroy(gameObject);
    }

    public override void Spawned()
    {
        if (!HasStateAuthority)
        {            
            BuildZoneGroups();
        }        
    }    


    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if(!HasStateAuthority) return;
        ClearZoneData();

        // Random noise offset mỗi lần generate
        noiseOffset = new Vector2(
            UnityEngine.Random.Range(1f, 10000f),
            UnityEngine.Random.Range(1f, 10000f)
        );
        Debug.Log($"[MapManager] Noise Offset randomized: {noiseOffset}");

        BuildZoneGroups();
    }

    [ContextMenu("Clear Zone Data")]
    public void ClearZoneData()
    {
        AllZoneGroups.Clear();
        ZoneGroupsByType.Clear();
        zoneGrid = null;
    }

    // ===================== Zone Classification =====================

    private void BuildZoneGroups()
    {
        AllZoneGroups.Clear();
        ZoneGroupsByType.Clear();

        ZoneGroupsByType[ZoneType.Green] = new List<ZoneGroup>();
        ZoneGroupsByType[ZoneType.Yellow] = new List<ZoneGroup>();
        ZoneGroupsByType[ZoneType.Red] = new List<ZoneGroup>();

        gridWidth = Mathf.CeilToInt(mapWidth / zoneCellSize);
        gridHeight = Mathf.CeilToInt(mapHeight / zoneCellSize);

        zoneGrid = new ZoneType[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float worldX = x * zoneCellSize + zoneCellSize * 0.5f;
                float worldZ = y * zoneCellSize + zoneCellSize * 0.5f;
                zoneGrid[x, y] = GetZoneTypeAt(worldX, worldZ);
            }
        }

        bool[,] visited = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (visited[x, y]) continue;

                ZoneType type = zoneGrid[x, y];
                ZoneGroup group = FloodFill(x, y, type, visited);

                if (group.CellCount > 0)
                {
                    AllZoneGroups.Add(group);
                    ZoneGroupsByType[type].Add(group);
                }
            }
        }

        Debug.Log($"[MapManager] Zone Groups built: Total={AllZoneGroups.Count} | " +
                  $"Green={ZoneGroupsByType[ZoneType.Green].Count} | " +
                  $"Yellow={ZoneGroupsByType[ZoneType.Yellow].Count} | " +
                  $"Red={ZoneGroupsByType[ZoneType.Red].Count}");
    }

    private ZoneGroup FloodFill(int startX, int startY, ZoneType type, bool[,] visited)
    {
        ZoneGroup group = new ZoneGroup { type = type };

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        Vector3 sumPos = Vector3.zero;
        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            group.cells.Add(cell);

            float worldX = cell.x * zoneCellSize + zoneCellSize * 0.5f;
            float worldZ = cell.y * zoneCellSize + zoneCellSize * 0.5f;
            Vector3 worldPos = new Vector3(worldX, 0f, worldZ);
            sumPos += worldPos;
            minX = Mathf.Min(minX, worldX - zoneCellSize * 0.5f);
            minZ = Mathf.Min(minZ, worldZ - zoneCellSize * 0.5f);
            maxX = Mathf.Max(maxX, worldX + zoneCellSize * 0.5f);
            maxZ = Mathf.Max(maxZ, worldZ + zoneCellSize * 0.5f);

            for (int i = 0; i < 4; i++)
            {
                int nx = cell.x + dx[i];
                int ny = cell.y + dy[i];

                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;
                if (visited[nx, ny])
                    continue;
                if (zoneGrid[nx, ny] != type)
                    continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        int count = group.CellCount;
        if (count > 0)
        {
            group.center = sumPos / count;
            group.bounds = new Bounds();
            group.bounds.SetMinMax(new Vector3(minX, 0f, minZ), new Vector3(maxX, 1f, maxZ));
            group.area = count * zoneCellSize * zoneCellSize;
        }

        return group;
    }

    // ===================== Public Query Helpers =====================

    public ZoneType GetZoneTypeAt(float x, float z)
    {
        float noiseValue = Mathf.PerlinNoise((x + noiseOffset.x) * noiseScale, (z + noiseOffset.y) * noiseScale);

        if (noiseValue >= redThreshold) return ZoneType.Red;
        if (noiseValue >= yellowThreshold) return ZoneType.Yellow;
        return ZoneType.Green;
    }

    public ZoneSpawnConfig GetZoneConfigAt(float x, float z)
    {
        ZoneType type = GetZoneTypeAt(x, z);
        switch (type)
        {
            case ZoneType.Red: return redZoneConfig;
            case ZoneType.Yellow: return yellowZoneConfig;
            default: return greenZoneConfig;
        }
    }

    public ZoneGroup GetZoneGroupAt(Vector3 worldPos)
    {
        int gx = Mathf.FloorToInt(worldPos.x / zoneCellSize);
        int gy = Mathf.FloorToInt(worldPos.z / zoneCellSize);

        if (gx < 0 || gx >= gridWidth || gy < 0 || gy >= gridHeight)
            return null;

        foreach (var group in AllZoneGroups)
        {
            if (group.cells.Contains(new Vector2Int(gx,gy)))
                return group;
        }
        return null;
    }

    public List<ZoneGroup> GetZoneGroups(ZoneType type)
    {
        if (ZoneGroupsByType.TryGetValue(type, out var list))
            return list;
        return new List<ZoneGroup>();
    }


    #region Helper

    public Vector3? FindValidSpawnPositionInGreen(
    Bounds objectBounds,
    LayerMask checkLayers,
    int maxAttempts = 80,
    float yOffset = 0f)
    {       

        var greenGroups = GetZoneGroups(ZoneType.Green);
        if (greenGroups == null || greenGroups.Count == 0)
        {
            Debug.LogWarning("[MapManager] Không có vùng Green nào!");
            return null;
        }

        // Tổng diện tích để weighted random (ưu tiên group lớn hơn)
        float totalArea = 0f;
        foreach (var g in greenGroups) totalArea += g.area;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 1. Chọn ngẫu nhiên 1 ZoneGroup (weighted theo area)
            ZoneGroup chosenGroup = null;
            float r = UnityEngine.Random.Range(0f, totalArea);
            float acc = 0f;
            foreach (var g in greenGroups)
            {
                acc += g.area;
                if (r <= acc)
                {
                    chosenGroup = g;
                    break;
                }
            }
            if (chosenGroup == null) chosenGroup = greenGroups[UnityEngine.Random.Range(0, greenGroups.Count)];

            // 2. Random vị trí trong group
            Vector3 candidate;
            if (chosenGroup.cells.Count > 0)
            {
                // Cách tốt: lấy random 1 cell rồi jitter nhẹ
                Vector2Int cellPos = chosenGroup.cells.RandomElement();
                Vector3 worldPos = new Vector3(cellPos.x, 0, cellPos.y) + Vector3.one * 0.5f;
                float jitter = zoneCellSize * 0.4f;
                candidate = worldPos + new Vector3(
                    UnityEngine.Random.Range(-jitter, jitter),
                    yOffset,
                    UnityEngine.Random.Range(-jitter, jitter)
                );
            }
            else
            {
                // Fallback dùng bounds
                Bounds b = chosenGroup.bounds;
                candidate = new Vector3(
                    UnityEngine.Random.Range(b.min.x, b.max.x),
                    yOffset,
                    UnityEngine.Random.Range(b.min.z, b.max.z)
                );
            }

            // 3. Kiểm tra vẫn còn nằm trong Green (tránh jitter ra ngoài)
            if (GetZoneTypeAt(candidate.x, candidate.z) != ZoneType.Green)
                continue;

            // 4. Kiểm tra va chạm với bounds
            if (IsPositionValid(candidate, objectBounds, checkLayers))
            {
                return candidate;
            }
        }

        Debug.LogWarning($"[MapManager] Không tìm được vị trí spawn hợp lệ sau {maxAttempts} lần thử.");
        return null;
    }

    /// <summary>
    /// Kiểm tra vị trí có bị chồng lên object khác không.
    /// </summary>
    private bool IsPositionValid(Vector3 worldPos, Bounds objectBounds, LayerMask checkLayers)
    {
        // Đưa bounds về đúng vị trí thế giới
        Vector3 halfExtents = objectBounds.extents;
        // Có thể thêm padding nếu muốn an toàn hơn
        // halfExtents += Vector3.one * 0.3f;

        Collider[] hits = Physics.OverlapBox(
            worldPos + objectBounds.center,   // nếu bounds.center không phải (0,0,0)
            halfExtents,
            Quaternion.identity,
            checkLayers
        );

        return hits.Length == 0;
    }

    #endregion



    // ===================== Debug Gizmos =====================

    private void OnDrawGizmosSelected()
    {
        if (!DrawDebug) return;
        if (greenZoneConfig == null || yellowZoneConfig == null || redZoneConfig == null)
            return;
        if (!Application.isPlaying) return;
        // Editor preview (khi chưa Generate)
        float stepSize = 1.0f;
        for (float x = 0; x < mapWidth; x += stepSize)
        {
            for (float z = 0; z < mapHeight; z += stepSize)
            {
                ZoneSpawnConfig config = GetZoneConfigAt(x, z);
                if (config != null)
                {
                    Gizmos.color = config.debugColor;
                    Vector3 center = new Vector3(x + stepSize / 2f, 0.01f, z + stepSize / 2f);
                    Vector3 size = new Vector3(stepSize, 0.02f, stepSize);
                    Gizmos.DrawCube(center, size);
                }
            }
        }
    }
}
