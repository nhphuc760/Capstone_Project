using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class DamageStructRequirement : StructRequirement
{


    public int RequireDamage;

    protected int damgeSupervise;

    protected System.Action<int> handler;


    public override UpgradeResult CheckRequirement()
    {
        if (damgeSupervise >= RequireDamage)
        {
            return new UpgradeResult { Reason = UpgradeFailReason.None, Message = "" };
        }
        return new UpgradeResult {Reason = UpgradeFailReason.NotEnoughDamage, Message = "Chưa gây đủ sát thương" };
    }

    public override StructRequirement CreateInstance(StructureBase structBase) 
    {
        if (structBase is not StructAttackBase structAttack)
        {
            Debug.LogWarning($"Không thể tạo DamageStructRequirement cho {structBase.StructureDataSO._name}");
            return null;
        }
        DamageStructRequirement damageStructRequirement = new DamageStructRequirement()
        {
            structAuthority = structBase,
            RequireDamage = this.RequireDamage,
            damgeSupervise = 0,            
        };
        damageStructRequirement.SuperviseDamage(structAttack);
        return damageStructRequirement;
    }

    public override void Destroy()
    {
        var attackStruct = structAuthority as StructAttackBase;
        if (attackStruct != null)
        {
            UnSuperviseDamage(attackStruct);
        }
        else
        {
            Debug.LogWarning("Lỗi cast StructAttackBase");
        }
    }

    public override string Information()
    {
        return $"Gây sát thương: " + $"{damgeSupervise}/{RequireDamage}".ToColor(damgeSupervise >= RequireDamage ? Color.green : Color.red);
    }

    protected void SuperviseDamage(StructAttackBase structAttackBase)
    {
        handler = (dmg) => { damgeSupervise += dmg; };
        structAttackBase.OnDamage += handler;
    }

    protected void UnSuperviseDamage(StructAttackBase structAttackBase)
    {
        structAttackBase.OnDamage -= handler;
    }

}
