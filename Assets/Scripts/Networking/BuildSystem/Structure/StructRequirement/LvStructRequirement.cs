using System;
using System.Linq;
using Fusion;
using UnityEngine;
[Serializable]
public class LvStructRequirement : RequirementDefinition
{
    public int Level;
    public StructureDataSO structureRequire;

    public override IRequirement CreateRuntime(RequirementServices services, NetworkObject source = default)
    {
        return new LvStructRequirementInstance
        {
            definition = this,
            structManager = services.structureManager,
        };
    }    
  
}

[Serializable]
public class LvStructRequirementInstance : IRequirement
{
    public LvStructRequirement definition;
    public StructureManager structManager;
    public PlayerRef playerRef;

    public event Action Changed;

    public void Dispose()
    {

    }

    public void Effect()
    {
    }

    public string GetInfor()
    {
        return $"{definition.structureRequire._name}: " + $"lv{definition.Level}".ToColor(Color.lightBlue);
    }

    public CheckMetResult Met()
    {
        var structuresByType = structManager.GetStructures().Where(x => x.Value.StructureDataSO == definition.structureRequire && x.Value.Object.InputAuthority == playerRef).Select(y => y.Value);
        if (structuresByType == null || structuresByType.Count() == 0)
        {
            return new CheckMetResult
            {
                Reason = CheckMetReason.NotEnoughLevel,
                Message = $"Yêu cầu {definition.structureRequire._name} Lv{definition.Level}"
            };
        }

        int levelCompare = definition.Level;
        if (!structuresByType.Any(x => x.Level >= levelCompare))
        {
            return new CheckMetResult
            {
                Reason = CheckMetReason.NotEnoughLevel,
                Message = $"Yêu cầu {definition.structureRequire._name} Lv{definition.Level}"
            };
        }
        return default;
    }

    public void Start()
    {

    }
}