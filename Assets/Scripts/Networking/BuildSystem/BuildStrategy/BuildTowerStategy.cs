using Fusion;
using UnityEngine;

public class BuildTowerStategy : IBuildStategy
{

    readonly StructureManager structureManager;
    StructureDataSO structDataSO;

    public BuildTowerStategy(StructureManager structureManager, StructureDataSO turretDataSO)
    {
        this.structureManager = structureManager;
        this.structDataSO = turretDataSO;
    }


    public void Build(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {        
        var structure = runner.Spawn(structDataSO.prefabs, new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f), inputAuthority: playerRef);
        structureManager.SetStructure(cell, structure.GetBehaviour<StructureBase>());
    }

    public BuildValidationResult CanBuild(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {      
        var enclosedZone = structureManager.GetEnclosedZone(playerRef);
        if (enclosedZone.Count < 1 || !enclosedZone.Contains(cell))
        {
            return BuildValidationResult.Fail(BuildFailReason.TurretNotInEnclosedZone, "Chỉ được xây trong nhà");
        }               
        return new BuildValidationResult { Reason = BuildFailReason.None, Message = "Oke build đi em" };

    }

    public void Destroy(NetworkRunner runner, NetworkObject obj)
    {
        obj.GetBehaviour<StructureBase>().DestroyLogic();
        structureManager.DestroyStructure(obj);
    }

    public void SetDataSO(StructureDataSO structureDataSO)
    {
        this.structDataSO = structureDataSO;
    }
}
