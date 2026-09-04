using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class WallBuildStrategy : IBuildStategy
{
    readonly StructureManager structureManager;
    readonly int _widthMap;
    readonly int _heightMap;
    readonly LayerMask _layerObstacleBuild;
    readonly StructureDataSO wallSO;
    readonly StructureDataSO doorSO;

    public WallBuildStrategy(StructureManager structureManager, int widthMap, int heightMap, LayerMask layerObstacleBuild, StructureDataSO wallSO, StructureDataSO doorSO)
    {
        
        this.structureManager = structureManager;
        _widthMap = widthMap;
        _heightMap = heightMap;
        _layerObstacleBuild = layerObstacleBuild;
        this.wallSO = wallSO;
        this.doorSO = doorSO;

    }
    public void Build(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {
        if (!runner.IsServer) return;
        var walls = structureManager.WithPlayerRef(playerRef).WithType(StructureType.Wall).Get();
        
        Utils.EditorLogOnly("wallsDoor Count = " + walls.Count);
        if (EnclosedChecker.CheckFromNewCell(cell, walls.Keys.ToHashSet(), out List<Vector3Int> enClosedList, _widthMap, _heightMap))
        {                        
            Vector3Int doorPos = EnclosedChecker.FindNearestStraightDegree2(cell, walls.Keys.ToHashSet());
            StructureBase doorObj = runner.Spawn(doorSO.prefabs, doorPos + Vector3.one * 0.5f, Quaternion.identity, playerRef).GetBehaviour<StructureBase>();
            if (doorPos != cell)
            {
                StructureBase wallAtDoorPos = walls[doorPos];
                wallAtDoorPos.transform.position = cell + Vector3.one * 0.5f;                
                structureManager.SetStructure(doorPos, doorObj);
                structureManager.SetStructure(cell, wallAtDoorPos);
            }
            else
            {
                structureManager.SetStructure(cell, doorObj);
            }
            structureManager.SetEnclosedZone(playerRef, enClosedList);
        }
        else
        {
            StructureBase wallSpawn = runner.Spawn(wallSO.prefabs, cell + Vector3.one * 0.5f, Quaternion.identity, playerRef).GetBehaviour<StructureBase>();
            structureManager.SetStructure(cell, wallSpawn);
        }
    }
    Collider[] results = new Collider[2];
   
    public BuildValidationResult CanBuild(NetworkRunner runner,PlayerRef playerRef, Vector3Int cell)
    {       
        Utils.EditorLogOnly("WallBuildStategy Check CanBuild");
        var doors = structureManager.WithType(StructureType.Door).Get();
        var myDoor = doors.Where(kvp => kvp.Value.Object.InputAuthority == playerRef);       
        if (myDoor.Count() != 0)
        {
            return new BuildValidationResult
            {
                Reason = BuildFailReason.DoorCompleted,
                Message = "Kiến trúc đã hoàn chỉnh"
            };
        }
        var neighbors = EnclosedChecker.GetNeighborS4Cell(cell, _widthMap, _heightMap);
        var neighborIsOtherPlayer = GetWallAndDoor().Where(kvp => { return neighbors.Contains(kvp.Key) && kvp.Value.Object.InputAuthority != playerRef; });
        if (neighborIsOtherPlayer.Count() != 0)
        {
            return new BuildValidationResult
            {
                Reason = BuildFailReason.InvalidPosition,
                Message = "👉 Bố cấm mày xây sát tường người khác. "
            };
        }

        //Check enclosed exist door
        var myWalls = structureManager.WithPlayerRef(playerRef).WithType(StructureType.Wall).Get();
        if(EnclosedChecker.CheckFromNewCell(cell, myWalls.Keys.ToHashSet(), out var EnclosedList, _widthMap, _heightMap))
        {
            foreach (var i in EnclosedList)
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
        Vector3 offset = new Vector3(-0.01f, 0, -0.01f);
        Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f) + offset;
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, _layerObstacleBuild);
        DrawLog.DrawCube(center, halfExtents, Color.blue, Time.deltaTime);
        if (count == 0)
        {
            return BuildValidationResult.OK();
        }
        else
        {
            return new BuildValidationResult
            {
                Reason = BuildFailReason.OccupiedByStructure,
                Message = "Không thể xây, có vật cản"
            };
        }            
    }



    Dictionary<Vector3Int, StructureBase> GetWallAndDoor()
    {
        if (structureManager != null)
        {
            return structureManager.GetStructures()
                .Where((kvp) => 
                { 
                    StructureDataSO structureDataSO = kvp.Value.StructureDataSO;

                    return structureDataSO.structureCategory == StructureCategory.Defense && (structureDataSO.structureType == StructureType.Wall || structureDataSO.structureType == StructureType.Door);
                }).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        return null;
    }
    
}
