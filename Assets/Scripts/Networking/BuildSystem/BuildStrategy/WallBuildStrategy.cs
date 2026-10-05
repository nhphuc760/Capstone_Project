using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Util;

public class WallBuildStrategy : IBuildStategy
{
    readonly StructureManager structureManager;
    readonly int _widthMap;
    readonly int _heightMap;
    readonly LayerMask _layerObstacleBuild;
    readonly StructureDataSO wallSO;
    readonly StructureDataSO doorSO;

    // Door tạm thời bị tắt khi vòng bị phá
    Door Door;

    /// <summary>
    /// Cache kết quả CheckFromNewCell giữa CanBuild → Build (cùng player + cell).
    /// Tránh chạy flood 2 lần trên server.
    /// </summary>
    struct EnclosureCache
    {
        public bool Valid;
        public PlayerRef Player;
        public Vector3Int Cell;
        public bool CreatesEnclosure;
        public int RegionCount;
        public List<Vector3Int> EnclosedList;
        public HashSet<Vector3Int> WallSet;
    }

    EnclosureCache _cache;

    public WallBuildStrategy(StructureManager structureManager, int widthMap, int heightMap,
        LayerMask layerObstacleBuild, StructureDataSO wallSO, StructureDataSO doorSO)
    {
        this.structureManager = structureManager;
        _widthMap = widthMap;
        _heightMap = heightMap;
        _layerObstacleBuild = layerObstacleBuild;
        this.wallSO = wallSO;
        this.doorSO = doorSO;
    }

    // =========================================================================
    // Phân tích enclosure – chỉ chạy 1 lần, cache lại
    // =========================================================================

    EnclosureCache AnalyzeAndCache(PlayerRef playerRef, Vector3Int cell)
    {
        if (_cache.Valid && _cache.Player == playerRef && _cache.Cell == cell)
            return _cache;

        var walls = structureManager.WithPlayerRef(playerRef).WithType(StructureType.Wall).Get();
        var wallSet = walls.Keys.ToHashSet();

        bool creates = EnclosedChecker.CheckFromNewCell(
            cell, wallSet, out var enclosedList, out int regionCount, _widthMap, _heightMap);

        _cache = new EnclosureCache
        {
            Valid = true,
            Player = playerRef,
            Cell = cell,
            CreatesEnclosure = creates,
            RegionCount = regionCount,
            EnclosedList = enclosedList ?? new List<Vector3Int>(),
            WallSet = wallSet
        };
        return _cache;
    }

    void InvalidateCache()
    {
        _cache.Valid = false;
    }

    // =========================================================================
    // BUILD
    // =========================================================================

    public void Build(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {
        if (!runner.IsServer) return;

        // Tái sử dụng cache từ CanBuild; miss thì analyze lại
        var analysis = AnalyzeAndCache(playerRef, cell);
        InvalidateCache();

        var walls = structureManager.WithPlayerRef(playerRef).WithType(StructureType.Wall).Get();
        HashSet<Vector3Int> wallSet = analysis.WallSet ?? walls.Keys.ToHashSet();

        bool createsEnclosure = analysis.CreatesEnclosure;
        int regionCount = analysis.RegionCount;
        List<Vector3Int> enclosedList = analysis.EnclosedList;

        if (createsEnclosure && regionCount > 1)
        {
            Debug.LogWarning($"[Build] Rejected: would create {regionCount} enclosed regions (only 1 allowed).");
            return;
        }

        if (createsEnclosure && regionCount == 1)
        {
            StructureManager.Ins?.ClearEscapeWall(playerRef);
            Vector3Int doorPos = EnclosedChecker.FindNearestStraightDegree2(cell, wallSet);

            StructureBase doorObj;
            if (Door != null)
            {
                Door.RPC_SetActiveNetworked(true);
                doorObj = Door;
            }
            else
            {
                doorObj = runner.Spawn(doorSO.prefabs, doorPos + Vector3.one * 0.5f, Quaternion.identity, playerRef)
                    .GetBehaviour<StructureBase>();
            }

            doorObj.GetBehaviour<NetworkTransform>().Teleport(doorPos + Vector3.one * 0.5f);

            if (doorPos != cell)
            {
                StructureBase wallAtDoorPos = walls[doorPos];
                wallAtDoorPos.Object.GetComponent<NetworkTransform>().Teleport(cell + Vector3.one * 0.5f);

                structureManager.SetStructure(doorPos, doorObj);
                structureManager.SetStructure(cell, wallAtDoorPos);
            }
            else
            {
                Debug.Log("Wall không ở góc chết – đặt door tại cell mới");
                structureManager.SetStructure(cell, doorObj);
            }
            structureManager.SetEnclosedZone(playerRef, enclosedList);
            EventBus<BuildStategyEvent>.Raise(new BuildStategyEvent
            {
                PlayerRef = playerRef,
                BuildType = BuildStategyEvent.BuildEventType.Build,
                StructureCategory = StructureCategory.Defense,
                StructureType = StructureType.Door,
                cellPosition = cell,
                bounds = doorObj.GetComponent<Collider>().bounds
            });
        }
        else
        {
            StructureBase wallSpawn = runner.Spawn(wallSO.prefabs, cell + Vector3.one * 0.5f, Quaternion.identity, playerRef)
                .GetBehaviour<StructureBase>();
            structureManager.SetStructure(cell, wallSpawn);

            List<Vector3Int> escapeWalls = EnclosedChecker.GetEscapeWalls(cell, wallSet, _widthMap, _heightMap);
            if (escapeWalls.Count > 0)
            {
                Debug.Log($"[AlmostEnclosed] Escape walls: {string.Join(", ", escapeWalls)}");
                StructureManager.Ins?.SetEscapesWall(playerRef, escapeWalls.ToArray());
            }
            else
            {
                StructureManager.Ins?.ClearEscapeWall(playerRef);
            }            
        }
       
    }

    // =========================================================================
    // CAN BUILD
    // =========================================================================

    Collider[] results = new Collider[2];

    public BuildValidationResult CanBuild(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {
        var doors = structureManager.WithType(StructureType.Door).Get();
        var myDoor = doors.Where(kvp => kvp.Value.Object.InputAuthority == playerRef);
        if (myDoor.Any())
        {
            InvalidateCache();
            return new BuildValidationResult
            {
                Reason = BuildFailReason.DoorCompleted,
                Message = "Kiến trúc đã hoàn chỉnh"
            };
        }

        var neighbors = EnclosedChecker.GetNeighborS4Cell(cell, _widthMap, _heightMap);
        var neighborIsOtherPlayer = GetWallAndDoor()
            .Where(kvp => neighbors.Contains(kvp.Key) && kvp.Value.Object.InputAuthority != playerRef);
        if (neighborIsOtherPlayer.Any())
        {
            InvalidateCache();
            return new BuildValidationResult
            {
                Reason = BuildFailReason.InvalidPosition,
                Message = "👉 Bố cấm mày xây sát tường người khác."
            };
        }

        // Phân tích 1 lần → cache cho Build
        var analysis = AnalyzeAndCache(playerRef, cell);

        if (analysis.CreatesEnclosure)
        {
            if (analysis.RegionCount > 1)
            {
                return new BuildValidationResult
                {
                    Reason = BuildFailReason.MultipleEnclosedZones,
                    Message = "Chỉ được tạo 1 vùng an toàn. Không được xây thành hình số 8 (nhiều vùng kín)."
                };
            }

            foreach (var i in analysis.EnclosedList)
            {
                if (doors.ContainsKey(i))
                {
                    return new BuildValidationResult
                    {
                        Reason = BuildFailReason.OtherDoorInSafeZone,
                        Message = "Đã có người chơi xây thành trong khu an toàn, Hãy chọn nơi khác"
                    };
                }
            }
        }

        Vector3 center = cell + Vector3.one * 0.5f;
        Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f) + new Vector3(-0.01f, 0, -0.01f);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, _layerObstacleBuild);
        DrawLog.DrawCube(center, halfExtents, Color.blue, Time.deltaTime);

        if (count == 0)
            return BuildValidationResult.OK();

        InvalidateCache();
        return new BuildValidationResult
        {
            Reason = BuildFailReason.OccupiedByStructure,
            Message = "Không thể xây, có vật cản"
        };
    }

    // =========================================================================
    // DESTROY
    // =========================================================================

    public void Destroy(NetworkRunner runner, NetworkObject obj)
    {
        Debug.Log("Destroy called");
        if (!runner.IsServer || obj == null) return;

        InvalidateCache();

        var wallsAndDoor = GetWallAndDoor();

        var structureEntry = wallsAndDoor.FirstOrDefault(x => x.Value.Object == obj);
        if (structureEntry.Value == null)
        {
            Debug.LogWarning("Không tìm thấy Structure tương ứng");
            return;
        }
        Vector3Int cell = structureEntry.Key;        
        if (!obj.TryGetBehaviour<Wall>(out _)) return;        
        var doorEntry = wallsAndDoor.FirstOrDefault(kvp => kvp.Value.StructureDataSO.structureType == StructureType.Door);
        var door = doorEntry.Value as Door;

        HashSet<Vector3Int> remaining = wallsAndDoor.Keys.ToHashSet();
        remaining.Remove(cell);

        bool stillEnclosed = false;
        List<Vector3Int> newEnclosedList = new List<Vector3Int>();

        if (remaining.Count > 0)
            stillEnclosed = EnclosedChecker.CheckExisting(remaining, out newEnclosedList, _widthMap, _heightMap);

        if (!stillEnclosed)
        {
            structureManager.ClearEnclosedZone(obj.InputAuthority);

            if (door != null)
            {
                Debug.Log("Set door active false");
                remaining.Remove(doorEntry.Key);
                door.RPC_SetActiveNetworked(false);
                Door = door;
                structureManager.RemoveStructure(doorEntry.Key);              
            }
            Debug.Log("Vòng kín bị phá vỡ → Clear EnclosedList");
        }
        else
        {
            structureManager.SetEnclosedZone(obj.InputAuthority, newEnclosedList);
        }
        PlayerRef playerRefObj = obj.InputAuthority;
        structureManager.DestroyStructure(obj);       
    }

    Dictionary<Vector3Int, StructureBase> GetWallAndDoor()
    {
        if (structureManager == null) return new Dictionary<Vector3Int, StructureBase>();

        return structureManager.GetStructures()
            .Where(kvp =>
            {
                var data = kvp.Value.StructureDataSO;
                return data.structureCategory == StructureCategory.Defense &&
                       (data.structureType == StructureType.Wall || data.structureType == StructureType.Door);
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
