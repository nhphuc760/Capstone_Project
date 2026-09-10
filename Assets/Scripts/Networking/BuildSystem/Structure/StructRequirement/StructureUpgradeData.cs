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
        // An toàn khi requirements null hoặc rỗng
        if (requirements == null || requirements.Length == 0)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.None,
                Message = ""
            };
        }

        foreach (var req in requirements)
        {
            // Bỏ qua phần tử null
            if (req == null) continue;

            UpgradeResult result = req.CheckRequirement();
            if (!result.Success)
                return result;
        }

        return new UpgradeResult
        {
            Reason = UpgradeFailReason.None,
            Message = ""
        };
    }

    public override StructRequirement CreateInstance(StructureBase structBase)
    {
        // Không mutate object gốc
        var clone = new StructureUpgradeData
        {
            structAuthority = structBase
        };

        // Clone requirements an toàn
        if (requirements == null || requirements.Length == 0)
        {
            clone.requirements = Array.Empty<StructRequirement>();
            return clone;
        }

        // Lọc null + clone từng phần tử
        clone.requirements = requirements
            .Where(x => x != null)
            .Select(x => x.CreateInstance(structBase))
            .Where(x => x != null)          // phòng trường hợp Clone trả về null
            .ToArray();

        return clone;
    }

    public override void Destroy()
    {
        if (requirements == null) return;

        foreach (var req in requirements)
        {
            req?.Destroy();
        }

        requirements = null;
    }

    public override string Information()
    {
        if (requirements == null || requirements.Length == 0)
            return string.Empty;

        return string.Join("\n",
            requirements
                .Where(x => x != null)
                .Select(x => x.Information())
                .Where(s => !string.IsNullOrEmpty(s))
        );
    }
}
