using System.Linq;
using Fusion;
using UnityEngine;

public class Turret : StructAttackBase
{
    [Networked] TickTimer AttackSpeedTimer { get; set; }
    [SerializeField] Transform bulletSpawnPoint;
    [SerializeField] Transform gun;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] LayerMask EnemyMask;
    [SerializeField] float angularSpeed = 10f;
    Transform Target;
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
            var target = Physics.OverlapSphere(bulletSpawnPoint.position, range, EnemyMask).FirstOrDefault();
            Target = target != null ? target.transform : null;           
            if (target != null && HasStateAuthority)
            {
                Debug.Log("Turret spawn Bullet");
                Bullet bu = Runner.Spawn(bulletPrefab, bulletSpawnPoint.position);
                Debug.Log("AttackSpeed: " + _structureStats.Get(StatsType.AttackSpeed));
                bu.InitMission(_structureStats.Get(StatsType.Damage), 10f * _structureStats.Get(StatsType.AttackSpeed), Object, target.GetComponent<NetworkObject>());
            }
        }        
    }


    public override void Render()
    {
        if(Target != null)
        {
            Debug.Log("Target name: " + Target.name);
            Vector3 dir = (Target.position - transform.position);
            dir.y = 0;
            gun.rotation = Quaternion.Lerp(gun.rotation, Quaternion.LookRotation(dir.normalized), angularSpeed * Runner.DeltaTime);
         
        }
    }



    public override void UpgradeLogic()
    {

    }

    public override string GetInfor()
    {
        return $"Sát thương: {_structureStats.Get(StatsType.Damage)}\n" +
            $"Phạm vi: {_structureStats.Get(StatsType.Range)}\n" +
            $"Tốc độ: {_structureStats.Get(StatsType.AttackSpeed)}";
    }





}
