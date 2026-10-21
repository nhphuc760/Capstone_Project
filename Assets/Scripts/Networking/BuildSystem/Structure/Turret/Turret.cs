using UnityEngine;

public class Turret : StructAttackBase
{

    public override void Spawned()
    {
        base.Spawned();
    }

    public override void Operation()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

    }

    public override void UpgradeLogic()
    {

    }

    public override string GetInfor()
    {
        return $"Damage: {_structureStats.Get(StatsType.Damage)}\n" +
            $"Range: {_structureStats.Get(StatsType.Range)}\n" +
            $"Speed: {_structureStats.Get(StatsType.AttackSpeed)}";
    }
}
