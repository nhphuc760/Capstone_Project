using System;
using UnityEngine;

[Serializable] 
class KnockbackEffect : AbilityEffect
{
    public float force;
    public override void Execute(GameObject caster, GameObject target)
    {
        var dir = (target.transform.position - caster.transform.position).normalized;
        if(target.TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody.AddForce(dir * force, ForceMode.Impulse);
        }
    }
}