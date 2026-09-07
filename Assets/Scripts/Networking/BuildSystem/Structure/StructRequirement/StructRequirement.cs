using System;
using Fusion;
using UnityEngine;

[Serializable]
public abstract class StructRequirement
{
    protected StructureBase structAuthority;
    public StructRequirement()
    {
        
    }


    public abstract StructRequirement Clone(StructureBase structBase);

    public abstract UpgradeResult CheckRequirement();

    public abstract void Destroy();
    public abstract string Information();

}

public enum UpgradeFailReason
{
    None,
    NotEnoughResource,
    MissingRequirement,
    NotEnoughDamage,
    NonUpgradable,
    MaxLevel
}

public struct UpgradeResult
{
    public bool Success => Reason == UpgradeFailReason.None;
    public UpgradeFailReason Reason;
    public string Message;
}
