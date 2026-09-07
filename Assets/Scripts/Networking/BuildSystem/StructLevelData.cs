using System;
using UnityEngine;

[Serializable]
public class StructLevelData
{
    public int Level;
    public StatsBase Stats;
    public StructureUpgradeData[] upgrades;
}
