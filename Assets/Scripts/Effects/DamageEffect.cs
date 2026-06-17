using System;
using UnityEngine;

[Serializable]
class DamageEffect: AbilityEffect
{
    public int amount;
    public override void Execute(GameObject caster, GameObject target)
    {
        //target.TryGetComponent<Health>().ApplyDamage(amount);
    }
}
