using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class StructureUpgradeData : StructRequirement
{
    //public ResourceRequirement cost;
    [SerializeReference]
    public StructRequirement[] requirements;

    public override UpgradeResult CheckRequirement()
    {
        foreach (var i in requirements)
        {
            UpgradeResult result = i.CheckRequirement();
            if(!result.Success) return result;
        }
        return new UpgradeResult { Reason = UpgradeFailReason.None, Message = "" };
    }

    public override StructRequirement Clone(StructureBase structBase)
    {
        this.structAuthority = structBase;
        return new StructureUpgradeData
        {
            structAuthority = structBase,
            //cost = this.cost.Clone(structBase) as ResourceRequirement,
            requirements = this.requirements.Select(x => x.Clone(structBase)).ToArray()
        };
    }

    public override void Destroy()
    {
        foreach (var i in requirements)
        {
            i.Destroy();
        }

    }

    public override string Information()
    {
        string s = string.Empty;
        foreach (var i in requirements)
        {
            s += i.Information() + '\n';
        }
        return s.TrimEnd('\n');
    }
}
