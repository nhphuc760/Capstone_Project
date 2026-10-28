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
    StructureDataSO wallSO;
    BuildSystem buildSystem;
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

    public WallBuildStrategy(StructureManager structureManager, int widthMap, int heightMap, StructureDataSO wallSO)
    {
        this.structureManager = structureManager;
        _widthMap = widthMap;
        _heightMap = heightMap;
        this.wallSO = wallSO;
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
            Debug.LogError("Cell: " + cell);
            Vector3Int doorPos = EnclosedChecker.FindNearestStraightDegree2(cell, wallSet,filter: w =>  EnclosedChecker.IsValidDoorPosition(w, wallSet ,enclosedList));


            var playerObject = runner.GetPlayerObject(playerRef).GetBehaviour<NetworkPlayer>();

            Door doorObj = playerObject.Door;            
            if (doorObj != null)
            {
                doorObj.RPC_SetActiveNetworked(true);
            }                        
            doorObj.GetBehaviour<NetworkTransform>().Teleport(doorPos + new Vector3(.5f, 0f, .5f));
            if (doorPos != cell)
            {
                StructureBase wallAtDoorPos = walls[doorPos];
                wallAtDoorPos.Object.GetComponent<NetworkTransform>().Teleport(cell + new Vector3(0.5f, 0f, .5f));

                structureManager.SetStructure(doorPos, doorObj);
                structureManager.SetStructure(cell, wallAtDoorPos);
            }
            else
            {
                Debug.Log("Wall không ở góc chết – đặt door tại cell mới");
                structureManager.SetStructure(cell, doorObj);
            }
            structureManager.SetEnclosedZone(playerRef, enclosedList);
            Vector3 doorOppositeOutside = EnclosedChecker.GetOutsideCellOppositeDoor(doorPos, walls.Keys.ToHashSet(), enclosedList) + new Vector3(0.5f, 0, 0.5f);     
            doorObj.SetOutSideOppositeDoor(doorOppositeOutside);
            EventBus<BuildStategyEvent>.Raise(new BuildStategyEvent
            {
                PlayerRef = playerRef,
                BuildType = BuildStategyEvent.BuildEventType.Build,
                StructureCategory = StructureCategory.Defense,
                StructureType = StructureType.Door,
                cellPosition = cell,
            });
        }
        else
        {
            StructureBase wallSpawn = runner.Spawn(wallSO.prefabs, new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f), Quaternion.identity, playerRef)
                .GetBehaviour<StructureBase>();
            structureManager.SetStructure(cell, wallSpawn);

            List<Vector3Int> escapeWalls = EnclosedChecker.GetEscapeWalls(cell, wallSet, _widthMap, _heightMap);
            if (escapeWalls.Count > 0)
            {
                StructureManager.Ins?.SetEscapesWall(playerRef, escapeWalls.ToArray());
            }
            else
            {
                StructureManager.Ins?.ClearEscapeWall(playerRef);
            }            
        }
        MakeSureBuildSystemValid(runner, playerRef);
        buildSystem.wallAmount -= 1;                    
    }

    void MakeSureBuildSystemValid(NetworkRunner runner, PlayerRef playerRef)
    {
        if (buildSystem == null)
        {
            buildSystem = runner.GetPlayerObject(playerRef).GetBehaviour<BuildSystem>();
        }
    }

    // =========================================================================
    // CAN BUILD
    // =========================================================================
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

        if (cell.x < 1 || cell.x > _widthMap - 1 || cell.z < 1 || cell.z > _heightMap - 1)
        {
            return new BuildValidationResult
            {
                Reason = BuildFailReason.InvalidPosition,
                Message = "Vị trí nằm ngoài phạm vi"
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

        MakeSureBuildSystemValid(runner, playerRef);
        if(buildSystem.wallAmount < 1)
        {
            return BuildValidationResult.Fail(reason: BuildFailReason.NotEnoughResource, message: "Đã hết Wall");
        }
       
        return BuildValidationResult.OK();

    }

    // =========================================================================
    // DESTROY
    // =========================================================================

    public void Destroy(NetworkRunner runner, NetworkObject obj)
    {
        Debug.Log("Destroy called");
        if (!runner.IsServer || obj == null) return;


        if (structureManager.GetEscapesWall().Contains(obj))
        {
            structureManager.RemoveEscapeWall(obj.InputAuthority, obj);
        }

        InvalidateCache();

        var wallsAndDoor = GetWallAndDoor();

        var structureEntry = wallsAndDoor.FirstOrDefault(x => x.Value.Object == obj);
        if (structureEntry.Value == null)
        {
            Debug.LogWarning("Không tìm thấy Structure tương ứng");
            return;
        }
        Vector3Int cell = structureEntry.Key;        
        if (!obj.TryGetBehaviour<Wall>(out var wall)) return;        
        var playerObject = runner.GetPlayerObject(obj.InputAuthority).GetBehaviour<NetworkPlayer>();
        Door door = playerObject.Door;

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
                remaining.Remove(door.transform.position.ToInt());
                door.RPC_SetActiveNetworked(false);
                structureManager.RemoveStructure(door.transform.position.ToInt());              
            }
            Debug.Log("Vòng kín bị phá vỡ → Clear EnclosedList");
        }
        else
        {
            structureManager.SetEnclosedZone(obj.InputAuthority, newEnclosedList);
        }
        PlayerRef playerRefObj = obj.InputAuthority;
        MakeSureBuildSystemValid(runner, playerRefObj);
        buildSystem.wallAmount += 1;
        wall.DestroyLogic();
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

    public void SetDataSO(StructureDataSO structureDataSO)
    {
        this.wallSO =  structureDataSO;
    }
}
