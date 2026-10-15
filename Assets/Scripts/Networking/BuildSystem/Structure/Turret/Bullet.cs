using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public Stats bulletStats;
    NetworkObject turret;
    NetworkObject target;
    NetworkTransform networkTransform;

    public void SetStats(float damage, float speed, NetworkObject turretOwn, NetworkObject target)
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
            Vector3 pos = Vector3.MoveTowards(transform.position, target.transform.position, speed * Runner.DeltaTime);
            networkTransform.Teleport(pos);
        }
    }

    void OnTriggerTakedamage()
    {

    }

}
