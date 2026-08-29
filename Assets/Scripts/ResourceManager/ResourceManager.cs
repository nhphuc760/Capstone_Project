using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ResourceManager : NetworkBehaviour
{
    [Header("Map Boundaries")]
    public float mapWidth = 50f;
    public float mapHeight = 50f;

    [Header("Noise / Biome Settings")]
    public float noiseScale = 0.05f;
    public Vector2 noiseOffset;
    [Range(0, 1)] public float yellowThreshold = 0.45f;
    [Range(0, 1)] public float redThreshold = 0.70f;

    [Header("Zone Configurations")]
    public ZoneSpawnConfig greenZoneConfig;
    public ZoneSpawnConfig yellowZoneConfig;
    public ZoneSpawnConfig redZoneConfig;

    [Header("Poisson Settings")]
    public int numSamplesBeforeRejection = 30;

    [Header("Container")]
    public Transform objectsParent;


    private struct PoissonPoint
    {
        public Vector2 position;
        public float radius;
        public ZoneSpawnConfig config;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Debug.Log("Create map");
            GenerateMap();
        }
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ClearMap();

        if (objectsParent == null)
        {
            objectsParent = new GameObject("Generated_Objects").transform;
            objectsParent.SetParent(transform);
        }

        List<PoissonPoint> points = RunPoissonSampling();

        // Instantiate Prefabs dựa trên các điểm đã tính toán
        foreach (var p in points)
        {
            NetworkObject prefab = p.config.GetRandomItem();
            if (prefab == null) continue;

            Vector3 spawnPosition = new Vector3(p.position.x, 0f, p.position.y);
            Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Runner.Spawn(prefab, spawnPosition, spawnRotation, onBeforeSpawned: (runner, obj) =>
            {
                obj.transform.parent = objectsParent;
                obj.GetBehaviour<ResourceNode>().postition = spawnPosition;
            });
            // Tìm thông tin scale tương ứng từ Config
            //SpawnableItem itemData = p.config.spawnableItems.Find(i => i.prefab == prefab);
            //if (itemData.scaleMin != Vector3.zero && itemData.scaleMax != Vector3.zero)
            //{
            //    obj.transform.localScale = new Vector3(
            //        Random.Range(itemData.scaleMin.x, itemData.scaleMax.x),
            //        Random.Range(itemData.scaleMin.y, itemData.scaleMax.y),
            //        Random.Range(itemData.scaleMin.z, itemData.scaleMax.z)
            //    );
            //}
        }
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        if (objectsParent != null)
        {
            DestroyImmediate(objectsParent.gameObject);
            objectsParent = null;
        }
    }

    private ZoneSpawnConfig GetZoneConfigAt(float x, float z)
    {
        float noiseValue = Mathf.PerlinNoise((x + noiseOffset.x) * noiseScale, (z + noiseOffset.y) * noiseScale);

        if (noiseValue >= redThreshold) return redZoneConfig;
        if (noiseValue >= yellowThreshold) return yellowZoneConfig;
        return greenZoneConfig;
    }

    private List<PoissonPoint> RunPoissonSampling()
    {
        List<PoissonPoint> points = new List<PoissonPoint>();
        List<Vector2> activeSamples = new List<Vector2>();

        float minGlobalRadius = Mathf.Min(greenZoneConfig.radius,
                                Mathf.Min(yellowZoneConfig.radius, redZoneConfig.radius));
        float cellSize = minGlobalRadius / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt(mapWidth / cellSize);
        int gridHeight = Mathf.CeilToInt(mapHeight / cellSize);

        int[,] grid = new int[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                grid[x, y] = -1;

        // Điểm đầu tiên
        Vector2 firstPos = new Vector2(Random.Range(0, mapWidth), Random.Range(0, mapHeight));
        ZoneSpawnConfig firstConfig = GetZoneConfigAt(firstPos.x, firstPos.y);

        PoissonPoint firstPoint = new PoissonPoint { position = firstPos, radius = firstConfig.radius, config = firstConfig };
        points.Add(firstPoint);
        activeSamples.Add(firstPos);

        grid[(int)(firstPos.x / cellSize), (int)(firstPos.y / cellSize)] = 0;

        while (activeSamples.Count > 0)
        {
            int randomIndex = Random.Range(0, activeSamples.Count);
            Vector2 spawnCenter = activeSamples[randomIndex];
            ZoneSpawnConfig centerConfig = GetZoneConfigAt(spawnCenter.x, spawnCenter.y);

            bool accepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                float dirX = Mathf.Sin(angle);
                float dirY = Mathf.Cos(angle);
                float distance = Random.Range(centerConfig.radius, 2 * centerConfig.radius);

                Vector2 candidate = spawnCenter + new Vector2(dirX, dirY) * distance;

                if (IsValid(candidate, cellSize, grid, points))
                {
                    ZoneSpawnConfig candConfig = GetZoneConfigAt(candidate.x, candidate.y);
                    PoissonPoint newPoint = new PoissonPoint { position = candidate, radius = candConfig.radius, config = candConfig };

                    points.Add(newPoint);
                    activeSamples.Add(candidate);

                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count - 1;
                    accepted = true;
                    break;
                }
            }

            if (!accepted) activeSamples.RemoveAt(randomIndex);
        }

        return points;
    }

    private bool IsValid(Vector2 candidate, float cellSize, int[,] grid, List<PoissonPoint> points)
    {
        if (candidate.x < 0 || candidate.x >= mapWidth || candidate.y < 0 || candidate.y >= mapHeight)
            return false;

        ZoneSpawnConfig candConfig = GetZoneConfigAt(candidate.x, candidate.y);
        int cellX = (int)(candidate.x / cellSize);
        int cellY = (int)(candidate.y / cellSize);

        int searchRadius = Mathf.CeilToInt((candConfig.radius * 2) / cellSize);

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
    // Hàm này tự động chạy trong Editor khi bạn click chọn GameObject FlatMapGenerator
    private void OnDrawGizmosSelected()
    {
        // Chỉ vẽ Gizmos nếu đã gán đủ 3 Config
        if (greenZoneConfig == null || yellowZoneConfig == null || redZoneConfig == null)
            return;

        // Bước nhảy giữa các ô Preview (độ phân giải Gizmos)
        // Giảm xuống 0.5f để xem chi tiết hơn, hoặc tăng lên 2.0f nếu map quá to gây lag
        float stepSize = 1.0f;

        for (float x = 0; x < mapWidth; x += stepSize)
        {
            for (float z = 0; z < mapHeight; z += stepSize)
            {
                // 1. Lấy Config vùng tại tọa độ hiện tại
                ZoneSpawnConfig config = GetZoneConfigAt(x, z);

                if (config != null)
                {
                    // 2. Thiết lập màu vẽ bằng debugColor trong ScriptableObject
                    Gizmos.color = config.debugColor;

                    // 3. Vẽ một ô vuông/khối hộp mỏng nằm sát mặt đất
                    Vector3 center = new Vector3(x + stepSize / 2f, 0.01f, z + stepSize / 2f);
                    Vector3 size = new Vector3(stepSize, 0.02f, stepSize);

                    Gizmos.DrawCube(center, size);
                }
            }
        }
    }
}