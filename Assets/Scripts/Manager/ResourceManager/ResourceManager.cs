using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// Quản lý tài nguyên: chạy Poisson sampling để spawn resource nodes
/// dựa trên zone data từ MapManager.
/// </summary>
public class ResourceManager : NetworkBehaviour
{
    public static ResourceManager Ins { get; private set; }

    [Header("References")]
    public MapManager mapManager;

    [Header("Poisson Settings")]
    public int numSamplesBeforeRejection = 30;

    [Header("Container")]
    public Transform objectsParent;

    // ===================== Runtime =====================
    private struct PoissonPoint
    {
        public Vector2 position;
        public float radius;
        public ZoneSpawnConfig config;
    }

    /// <summary>Danh sách NetworkObject resource đã spawn (runtime).</summary>
    public List<NetworkObject> SpawnedResources { get; private set; } = new List<NetworkObject>();


    [Networked]
    private NetworkBool resourcesInitialized { get; set; }

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else if (Ins != this) Destroy(gameObject);
    }

    public async override void Spawned()
    {
        if (mapManager == null)
            mapManager = MapManager.Ins;

        // Nếu MapManager đã sẵn sàng trước (thứ tự spawn khác nhau)
        if (Object.HasStateAuthority && mapManager != null && mapManager.IsMapReady && !resourcesInitialized)
        {
            InitializeResources();

        }
        if (!HasStateAuthority)
        {
            await UniTask.WaitUntil(() => resourcesInitialized);
            NavMeshBakeManager.Ins.BakeAll();
        }
    }

    /// <summary>
    /// Khởi tạo resource nodes bằng thuật toán Poisson Disk Sampling gốc.
    /// Gọi sau khi MapManager.GenerateMap() đã xong.
    /// </summary>
    [ContextMenu("Initialize Resources")]
    public void InitializeResources()
    {
        if (!Object.HasStateAuthority) return;
        if (resourcesInitialized) return;

        if (mapManager == null)
            mapManager = MapManager.Ins;

        if (mapManager == null || !mapManager.IsMapReady)
        {
            Debug.LogWarning("[ResourceManager] MapManager chưa sẵn sàng, bỏ qua InitializeResources.");
            return;
        }

        ClearResources();

        if (objectsParent == null)
        {
            objectsParent = new GameObject("Resource_Objects").transform;
            objectsParent.SetParent(transform);
        }

        List<PoissonPoint> points = RunPoissonSampling();

        foreach (var p in points)
        {
            NetworkObject prefab = p.config.GetRandomItem();
            if (prefab == null) continue;

            Vector3 spawnPosition = new Vector3(p.position.x, 0f, p.position.y);
            Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            NetworkObject spawned = Runner.Spawn(prefab, spawnPosition, spawnRotation);
            if (spawned != null)
                SpawnedResources.Add(spawned);
        }

        resourcesInitialized = true;
        if(NavMeshBakeManager.Ins != null)
        {
            NavMeshBakeManager.Ins.BakeAll();
        }
    }

    [ContextMenu("Clear Resources")]
    public void ClearResources()
    {
        // Xóa các object đã spawn (nếu còn)
        if (objectsParent != null)
        {
            DestroyImmediate(objectsParent.gameObject);
            objectsParent = null;
        }

        SpawnedResources.Clear();
        resourcesInitialized = false;
    }

    // ===================== Poisson Disk Sampling (logic gốc) =====================

    private List<PoissonPoint> RunPoissonSampling()
    {
        List<PoissonPoint> points = new List<PoissonPoint>();
        List<Vector2> activeSamples = new List<Vector2>();

        float mapWidth = mapManager.mapWidth;
        float mapHeight = mapManager.mapHeight;

        ZoneSpawnConfig green = mapManager.greenZoneConfig;
        ZoneSpawnConfig yellow = mapManager.yellowZoneConfig;
        ZoneSpawnConfig red = mapManager.redZoneConfig;

        float minGlobalRadius = Mathf.Min(green.radius, Mathf.Min(yellow.radius, red.radius));
        float cellSize = minGlobalRadius / Mathf.Sqrt(2);

        int gridW = Mathf.CeilToInt(mapWidth / cellSize);
        int gridH = Mathf.CeilToInt(mapHeight / cellSize);

        int[,] grid = new int[gridW, gridH];
        for (int x = 0; x < gridW; x++)
            for (int y = 0; y < gridH; y++)
                grid[x, y] = -1;

        // Điểm đầu tiên
        Vector2 firstPos = new Vector2(Random.Range(0f, mapWidth), Random.Range(0f, mapHeight));
        ZoneSpawnConfig firstConfig = mapManager.GetZoneConfigAt(firstPos.x, firstPos.y);

        PoissonPoint firstPoint = new PoissonPoint
        {
            position = firstPos,
            radius = firstConfig.radius,
            config = firstConfig
        };
        points.Add(firstPoint);
        activeSamples.Add(firstPos);
        grid[(int)(firstPos.x / cellSize), (int)(firstPos.y / cellSize)] = 0;

        while (activeSamples.Count > 0)
        {
            int randomIndex = Random.Range(0, activeSamples.Count);
            Vector2 spawnCenter = activeSamples[randomIndex];
            ZoneSpawnConfig centerConfig = mapManager.GetZoneConfigAt(spawnCenter.x, spawnCenter.y);

            bool accepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float dirX = Mathf.Sin(angle);
                float dirY = Mathf.Cos(angle);
                float distance = Random.Range(centerConfig.radius, 2f * centerConfig.radius);

                Vector2 candidate = spawnCenter + new Vector2(dirX, dirY) * distance;

                if (IsValid(candidate, cellSize, grid, points, mapWidth, mapHeight))
                {
                    ZoneSpawnConfig candConfig = mapManager.GetZoneConfigAt(candidate.x, candidate.y);
                    PoissonPoint newPoint = new PoissonPoint
                    {
                        position = candidate,
                        radius = candConfig.radius,
                        config = candConfig
                    };

                    points.Add(newPoint);
                    activeSamples.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count - 1;
                    accepted = true;
                    break;
                }
            }

            if (!accepted)
                activeSamples.RemoveAt(randomIndex);
        }

        return points;
    }

    private bool IsValid(Vector2 candidate, float cellSize, int[,] grid, List<PoissonPoint> points,
                         float mapWidth, float mapHeight)
    {
        if (candidate.x < 0 || candidate.x >= mapWidth || candidate.y < 0 || candidate.y >= mapHeight)
            return false;

        ZoneSpawnConfig candConfig = mapManager.GetZoneConfigAt(candidate.x, candidate.y);
        int cellX = (int)(candidate.x / cellSize);
        int cellY = (int)(candidate.y / cellSize);

        int searchRadius = Mathf.CeilToInt((candConfig.radius * 2f) / cellSize);

        int startX = Mathf.Max(0, cellX - searchRadius);
        int endX = Mathf.Min(grid.GetLength(0) - 1, cellX + searchRadius);
        int startY = Mathf.Max(0, cellY - searchRadius);
        int endY = Mathf.Min(grid.GetLength(1) - 1, cellY + searchRadius);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int pointIndex = grid[x, y];
                if (pointIndex != -1)
                {
                    PoissonPoint existingPoint = points[pointIndex];
                    float minDistance = (candConfig.radius + existingPoint.radius) / 2f;

                    if (Vector2.SqrMagnitude(candidate - existingPoint.position) < minDistance * minDistance)
                        return false;
                }
            }
        }

        return true;
    }

    // ===================== Public Query Helpers (ủy quyền MapManager) =====================

    public ZoneType GetZoneTypeAt(float x, float z)
    {
        if (mapManager == null) return ZoneType.Green;
        return mapManager.GetZoneTypeAt(x, z);
    }

    public MapManager.ZoneGroup GetZoneGroupAt(Vector3 worldPos)
    {
        if (mapManager == null) return null;
        return mapManager.GetZoneGroupAt(worldPos);
    }

    public List<MapManager.ZoneGroup> GetZoneGroups(ZoneType type)
    {
        if (mapManager == null) return new List<MapManager.ZoneGroup>();
        return mapManager.GetZoneGroups(type);
    }
}
