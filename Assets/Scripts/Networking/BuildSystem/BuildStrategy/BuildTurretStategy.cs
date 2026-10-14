using Fusion;
using UnityEngine;

public class BuildTurretStategy : IBuildStategy
{

    readonly StructureManager structureManager;
    readonly StructureDataSO turretDataSO;

    public BuildTurretStategy(StructureManager structureManager, StructureDataSO turretDataSO)
    {
        this.structureManager = structureManager;
        this.turretDataSO = turretDataSO;
    }


    public void Build(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {        
        runner.Spawn(turretDataSO.prefabs, new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f));
    }

    public BuildValidationResult CanBuild(NetworkRunner runner, PlayerRef playerRef, Vector3Int cell)
    {      
        var enclosedZone = structureManager.GetEnclosedZone(playerRef);
        if (enclosedZone.Count < 1 || !enclosedZone.Contains(cell))
        {
            return BuildValidationResult.Fail(BuildFailReason.TurretNotInEnclosedZone, "Chỉ được xây trong nhà");
        }       
        foreach (var i in turretDataSO.buildRequirement)
        {
            var checkBuildRequirement = i.CheckRequirement(runner, playerRef);
            if (!checkBuildRequirement.Success)
            {
                return BuildValidationResult.Fail(reason: checkBuildRequirement.Reason, message: checkBuildRequirement.Message);
            }
        }
        return new BuildValidationResult { Reason = BuildFailReason.None, Message = "Oke build đi em" };

    }

    public void Destroy(NetworkRunner runner, NetworkObject obj)
    {

    }
}
