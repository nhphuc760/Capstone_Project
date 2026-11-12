using System;
using System.Linq;
using Fusion;
using UnityEngine;
public abstract class StructureBase : NetworkBehaviour, IAffector, ITooltipContext
{
    
    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int Level { get; set; }


    public Stats _structureStats { get; set; }

    [Networked]
    public NetworkBool IsUpgradeable { get; private set; }
    public StructureDataSO StructureDataSO;


    ResourceRequirement PreviousResourceRequirement;
    public IRequirement[] CurUpgradeRequirement { get; private set; }



    public override void Spawned()
    {        

        if (StructureDataSO.levels == null || StructureDataSO.levels.Length == 0)
        {
            IsUpgradeable = false;
            return;
        }        
        Level = 1;
        Util.Core.Utils.DelayCall(1f, OnLevelChanged).Forget();
        _structureStats ??= GetComponent<Stats>();
        var firstLevelData = StructureDataSO.levels[0];
        _structureStats.SetStatsBase(firstLevelData.Stats);
    }


    public override void FixedUpdateNetwork()
    {
        _structureStats?.Tick(Runner.DeltaTime);
    }
    public CheckMetResult CanUpgrade()
    {
        if (!IsUpgradeable)
        {
            if(Level == 1)
                return new CheckMetResult
                {
                    Reason = CheckMetReason.NonUpgradable,
                    Message = "Công trình này không thể nâng cấp"
                };
            return new CheckMetResult
            {
                Reason = CheckMetReason.MaxLevel,
                Message = "Công trình đã đạt cấp tối đa"
            };
        }

        foreach (var i in CurUpgradeRequirement)
        {
            var result = i.Met();
            if (!result.Success)
            {
                return result;
            }
        }

        return default;       
    }
    /// <summary>
    /// Nâng cấp an toàn. Chỉ chạy khi CanUpgrade() thành công.
    /// </summary>
    public void Upgrade()
    {        
        if (!Object.HasStateAuthority)
           return;    
        // Tăng level
        Level++;
        _structureStats.SetStatsBase(StructureDataSO.levels[Level - 1].Stats);
        // Hủy requirement cũ        
        foreach (var i in CurUpgradeRequirement)
        {
            i.Dispose();
        }
        CurUpgradeRequirement = null;
        

        // Gọi logic nâng cấp riêng của từng loại công trình
        UpgradeLogic();
    }


    
    private void OnLevelChanged()
    {
        //Cập nhật UpgradeRequirement
        Debug.Log($"{gameObject.name} OnLevelChanged: Lv{Level}", this);
        if (Level < StructureDataSO.levels.Length)
        {
            var nextLevelData = StructureDataSO.levels[Level];
            if (nextLevelData != null && nextLevelData.upgradeRequirement != null)
            {
                //_currentUpgradeData = nextLevelData.upgradeRequirement.CreateInstance(this) as StructureUpgradeData;
                //IsUpgradeable = _currentUpgradeData != null;
                var services = Runner.GetPlayerObject(Object.InputAuthority).GetComponent<RequirementServices>();
                if(CurUpgradeRequirement != null) 
                PreviousResourceRequirement = CurUpgradeRequirement.Where(x => x is ResourceRequirementInstance y).Select(z => (z as ResourceRequirementInstance).definition).FirstOrDefault();
                CurUpgradeRequirement = nextLevelData.upgradeRequirement.Select(x => x.CreateRuntime(services, Object)).ToArray();
                IsUpgradeable = CurUpgradeRequirement != null && CurUpgradeRequirement.Length != 0;
            }
            else
            {
                IsUpgradeable = false;
            }
        }
        else
        {
            // Đã max level
            IsUpgradeable = false;
        }
    }

    public virtual void DestroyLogic()
    {
        
    }

    public abstract string GetInfor();

    public abstract void UpgradeLogic();
    

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequireUpgrade()
    {
        CheckMetResult result = CanUpgrade();
        if (!result.Success)
        {
            Debug.LogWarning($"[StructureBase] Không thể nâng cấp: {result.Message} ({result.Reason})");
            return;
        }
        Upgrade();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequireDestroy()
    {
        IBuildStategy stategy =  StructureManager.Ins.GetStrategyBuild(StructureDataSO._id, Object.InputAuthority);
        stategy.Destroy(Runner, Object);

    }

    public void AddModifier(Modifier mod)
    {
        if(HasStateAuthority)
            _structureStats.AddModifier(mod );
    }

    public void RemoveModifier(int _id)
    {
        if(HasStateAuthority)
            _structureStats.RemoveModifierByID( _id);
    }

    public string GetContent() => GetInfor();
        
    public virtual string GetInforUpgrade()
    {
        string s = null;
        foreach (var i in CurUpgradeRequirement)
        {
            s += i.GetInfor() + "\n";
        }
        return s;
    }    
    public virtual InforDestroy GetResourceDestroy()
    {

        if (Level > 0)
        {
            if(Level == 1)
            {
                var buildRequire = StructureDataSO.buildRequirement?.Where(x => x is ResourceRequirement).FirstOrDefault();
                if(buildRequire != null)
                {
                    
                    var halfResource = GetHalfResource(buildRequire as ResourceRequirement, out string s);                    
                    return new InforDestroy
                    {
                        info = s,
                        resources = halfResource
                    };
                }
                return default;
            }
            else
            {
                 if(PreviousResourceRequirement != null)
                {
                   
                    if(PreviousResourceRequirement != null)
                    {
                        var halfResource = GetHalfResource(PreviousResourceRequirement, out string s);
                        return new InforDestroy
                        {
                            info = s,
                            resources = halfResource
                        };
                    }
                    return default;
                }
                return default;
            }
        }
        else 
        {
            return default;
        }

      
    }


    NetworkResourcePlayer GetHalfResource(ResourceRequirement resource, out string toString)
    {
        var halfResouce = new NetworkResourcePlayer
        {
            Wood = resource.Wood / 2,
            Stone = resource.Stone / 2,
            GoldOre = resource.Gold / 2,
            Electric = resource.Electric / 2,
        };
        var iconTMP = resource.iconTMP;

        string s = string.Empty;
        if (iconTMP == null)
        {
            Debug.LogWarning("iconTMP null");
            iconTMP = Resources.LoadAll<IconTMP>("IconTextMeshPro").First();
        }

        s += halfResouce.Wood > 0 ? $"{iconTMP.WoodCode} {halfResouce.Wood}  " : "";
        s += halfResouce.Stone > 0 ? $"{iconTMP.StoneCode} {halfResouce.Stone}  " : "";
        s += halfResouce.GoldOre > 0 ? $"{iconTMP.GoldCode} {halfResouce.GoldOre}  " : "";
        s += halfResouce.Electric > 0 ? $"{iconTMP.ElectricCode} {halfResouce.Electric}" : "";

        toString = s;
        return halfResouce;
    }

}


public struct InforDestroy
{
    public string info;
    public NetworkResourcePlayer resources;
}

public abstract class StructAttackBase: StructureBase
{
    public event Action<float> OnDamage;        
}


