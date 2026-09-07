using System;
using System.Linq;
using Fusion;
using Unity.VisualScripting;

public abstract class StructureBase : NetworkBehaviour
{
    [Networked]
    public int Level {  get; set; }
    public StructureDataSO StructureDataSO;
    StructureUpgradeData _currentUpgradeData;
    //public StructureManager Manager { get; set; }
    public abstract void Operation();  

    public override void Spawned()
    {
        base.Spawned();
        Level = 1;
        _currentUpgradeData = StructureDataSO.levels.First().upgrades.First().Clone(this) as StructureUpgradeData;
    }

    public UpgradeResult CanUpgrade()
    {
        if (StructureDataSO.levels[Level - 1] == null || StructureDataSO.levels[Level - 1].upgrades.Length == 0) 
            return new UpgradeResult { Reason = UpgradeFailReason.NonUpgradable, Message = "" };

        if (Level > StructureDataSO.levels[Level - 1].upgrades.Length)
        {
            return new UpgradeResult { Reason = UpgradeFailReason.MaxLevel, Message = "Công trình đã đạt cấp tối đa" };
        }


        return _currentUpgradeData.CheckRequirement();
    }
    public void Upgrade()
    {
        if (!Object.HasStateAuthority) return;
        if (CanUpgrade().Success)
        {
            Level++;
            _currentUpgradeData.Destroy();

            _currentUpgradeData = StructureDataSO.levels[Level - 1].upgrades[Level - 1]?.Clone(this) as StructureUpgradeData;

        }
    }

    public virtual void Destroy()
    {
        //if (HasStateAuthority) Runner.Despawn(this.Object);
    }


    public abstract void UpgradeLogic();

}


public abstract class StructAttackBase: StructureBase
{
    public event Action<int> OnDamage;

}


