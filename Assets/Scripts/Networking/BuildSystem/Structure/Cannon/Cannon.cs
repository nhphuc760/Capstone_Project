using UnityEngine;

public class Cannon : StructureBase
{
    public override string GetInfor()
    {
        string s = "";

        s += $"Sát thương: {_structureStats.Get(StatsType.Damage)} \n";
        s += $"Phạm vi: {_structureStats.Get(StatsType.Range)} \n";
        s += $"Tốc độ: {_structureStats.Get(StatsType.AttackSpeed)}";
        return s;
    }

    public override void UpgradeLogic()
    {

    }


}
