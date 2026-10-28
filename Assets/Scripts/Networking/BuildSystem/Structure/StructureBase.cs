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

    IRequirement[] UpgradeRequirement { get; set; }



    public override void Spawned()
    {        
        if (StructureDataSO.levels == null || StructureDataSO.levels.Length == 0)
        {
            IsUpgradeable = false;
            return;
        }        
        Level = 1;
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

        foreach (var i in UpgradeRequirement)
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
        foreach (var i in UpgradeRequirement)
        {
            i.Dispose();
        }
        UpgradeRequirement = null;
        

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
                UpgradeRequirement = nextLevelData.upgradeRequirement.Select(x => x.CreateRuntime(services, Object)).ToArray();
                IsUpgradeable = UpgradeRequirement != null && UpgradeRequirement.Length != 0;
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
        

}


public abstract class StructAttackBase: StructureBase
{
    public event Action<float> OnDamage;        
}


