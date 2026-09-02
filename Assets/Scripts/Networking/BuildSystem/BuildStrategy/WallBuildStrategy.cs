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
    readonly LayerMask _groundMask;
    readonly StructureDataSO wallSO;
    readonly StructureDataSO doorSO;
    public WallBuildStrategy(StructureManager structureManager, int widthMap, int heightMap, LayerMask layerObstacleBuild, LayerMask groundMask, StructureDataSO wallSO, StructureDataSO doorSO)
    {
        this.structureManager = structureManager;
        _widthMap = widthMap;
        _heightMap = heightMap;
        _layerObstacleBuild = layerObstacleBuild;
        _groundMask = groundMask;
        this.wallSO = wallSO;
        this.doorSO = doorSO;
        
    }
    public void Build(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {
        if (!runner.IsServer) return;
        var wallsDoor = GetWallAndDoor();
        
        Debug.Log("wallsDoor Count = " + wallsDoor.Count);
        if (EnclosedChecker.CheckFromNewCell(cell, wallsDoor.Keys.ToHashSet(), out List<Vector3Int> enClosedList, _widthMap, _heightMap))
        {
            Vector3Int doorPos = EnclosedChecker.FindNearestStraightDegree2(cell, wallsDoor.Keys.ToHashSet(), (cell) =>
            {
                if(wallsDoor.TryGetValue(cell, out StructureBase wall))
                {
                    return playerRef == wall.Object.InputAuthority;
                }
                else
                {
                    return false;
                }
            });
            StructureBase doorObj = runner.Spawn(doorSO.prefabs, doorPos + Vector3.one * 0.5f, Quaternion.identity, playerRef).GetBehaviour<StructureBase>();
            if (doorPos != cell)
            {
                StructureBase wallAtDoorPos = wallsDoor[doorPos];
                wallAtDoorPos.transform.position = cell + Vector3.one * 0.5f;                
                structureManager.SetStructure(doorPos, doorObj);
                structureManager.SetStructure(cell, wallAtDoorPos);
            }
            else
            {
                structureManager.SetStructure(cell, doorObj);
            }
        }
        else
        {
            StructureBase wallSpawn = runner.Spawn(wallSO.prefabs, cell + Vector3.one * 0.5f, Quaternion.identity, playerRef).GetBehaviour<StructureBase>();
            structureManager.SetStructure(cell, wallSpawn);
        }
    }
    Collider[] results = new Collider[2];
   
    public bool CanBuild(NetworkRunner runner,PlayerRef playerRef, Vector3Int cell)
    {
        Vector3 center = cell + Vector3.one * 0.5f;
        Vector3 offset = new Vector3(-0.01f, 0, -0.01f);
        Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f) + offset;
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, _layerObstacleBuild);
        DrawLog.DrawCube(center, halfExtents, Color.blue, Time.deltaTime);
        if (count == 0) return true;
        return false;
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
