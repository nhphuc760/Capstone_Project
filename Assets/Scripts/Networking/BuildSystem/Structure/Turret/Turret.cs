using System.Linq;
using Fusion;
using UnityEngine;

public class Turret : StructAttackBase, IInteractable
{
    [Networked] TickTimer AttackSpeedTimer { get; set; }
    [SerializeField] Transform bulletSpawnPoint;
    [SerializeField] Transform gun;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] InteractionData interactionData;
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
        return $"Damage: {_structureStats.Get(StatsType.Damage)}\n" +
            $"Range: {_structureStats.Get(StatsType.Range)}\n" +
            $"Speed: {_structureStats.Get(StatsType.AttackSpeed)}";
    }



    public Transform InteractTransform => transform;

    public System.Action OnCompleted { get; set ; }


    public InteractionData Data(NetworkPlayer interactor) => interactionData;
    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
        if (interactor.equipTool != ToolType.Screwdriver)
        {
            return new InteractionResult(false);
        }
        return new InteractionResult(true);
    }

    public void OnInteract(InteractionContext ctx)
    {
        if (ctx.Phase == InteractPhase.Hold)
        {
            if (ctx.HoldTime >= interactionData.HoldTime)
            {
                StructureManager.Ins?.DestroyStructure(Object);
            }
        }
    }

}
