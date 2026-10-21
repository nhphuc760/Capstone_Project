using Fusion;
using NUnit.Framework;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public Stats bulletStats;
    NetworkObject turret;
    NetworkObject target;
    NetworkTransform networkTransform;

    public void InitMission(float damage, float speed, NetworkObject turretOwn, NetworkObject target)
    {
        this.target = target;
        this.turret = turretOwn;
        bulletStats.SetValueBase(StatsType.MoveSpeed, speed);
        bulletStats.SetValueBase(StatsType.Damage, damage);
    }

    public override void Spawned()
    {
        bulletStats ??= GetComponent<Stats>();
        networkTransform ??= GetComponent<NetworkTransform>();
    }

    public override void FixedUpdateNetwork()
    {
        bulletStats.Tick(Runner.DeltaTime);
        if(target != null)
        {
            float speed = bulletStats.Get(StatsType.MoveSpeed);
            Vector3 targetPos = target.transform.position + Vector3.up;
            Vector3 pos = Vector3.MoveTowards(transform.position, targetPos, speed * Runner.DeltaTime);
            networkTransform.Teleport(pos);
            if ((transform.position - targetPos).sqrMagnitude < 0.5f)
            {
                OnTriggerTakedamage();
            }
        }
    }

    void OnTriggerTakedamage()
    {
        if (target.TryGetComponent<ITakedamageable>(out var takedamageable))
        {
            takedamageable.TakeDamage(bulletStats.Get(StatsType.Damage), turret);
        }
        turret = null;
        target = null;
        Runner.Despawn(this.Object);
    }
}
