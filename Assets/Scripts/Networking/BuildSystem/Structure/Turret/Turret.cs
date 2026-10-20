using System.Linq;
using Fusion;
using UnityEngine;
using UtilityAI.Core;

public class Turret : StructAttackBase
{
    [Networked] TickTimer AttackSpeedTimer { get; set; }
    [SerializeField] Transform bulletSpawnPoint;
    [SerializeField] Bullet bulletPrefab;
    public override void Spawned()
    {
        base.Spawned();

    }   

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        float range = _structureStats.Get(StatsType.Range);
        if (AttackSpeedTimer.ExpiredOrNotRunning(Runner))
        {
            AttackSpeedTimer = TickTimer.CreateFromSeconds(Runner, 1/_structureStats.Get(StatsType.AttackSpeed));
            var anyTarget = GameObject.FindGameObjectsWithTag("Enemy").Where(x => (x.transform.position - transform.position).sqrMagnitude <= range * range).FirstOrDefault();
            if (anyTarget != null && HasStateAuthority)
            {
                Debug.Log("Turret spawn Bullet");
                Bullet bu = Runner.Spawn(bulletPrefab, bulletSpawnPoint.position);
                Debug.Log("AttackSpeed: " + _structureStats.Get(StatsType.AttackSpeed));
                bu.InitMission(_structureStats.Get(StatsType.Damage), 10f * _structureStats.Get(StatsType.AttackSpeed), Object, anyTarget.GetComponent<NetworkObject>());
            }
        }        
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
