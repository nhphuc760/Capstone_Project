using System;
using UnityEngine;
[Serializable]
public class LevelStructRequirement : StructRequirement
{
    public int Level;
    public StructureType StructureType;
    
    public override UpgradeResult CheckRequirement()
    {
         if(StructureManager.Ins != null)
        {
            var structType = StructureManager.Ins.WithPlayerRef(structAuthority.Object.InputAuthority).WithType(StructureType).Get();
            foreach (var i in structType)
            {
                StructureBase typeRequire = i.Value;
                if (typeRequire.Level >= Level)
                {
                    return new UpgradeResult { Reason = UpgradeFailReason.None};
                }
            }
            return new UpgradeResult { Reason = UpgradeFailReason.MissingRequirement, Message = $"Yêu cầu {StructureType.ToString()} Level {Level}" };
        }
        else
        {
            Debug.LogWarning("StructManager null");
            return default;
        }
    }

    public override StructRequirement Clone(StructureBase structBase)
    {
        return new LevelStructRequirement
        {
            structAuthority = structBase,
            Level = this.Level,
            StructureType = this.StructureType,
        };
    }

    public override void Destroy()
    {
       
    }

    public override string Information()
    {
        return $"{StructureType.ToString()}: " + $"lv{Level}".ToColor(Color.lightBlue);
    }
}
