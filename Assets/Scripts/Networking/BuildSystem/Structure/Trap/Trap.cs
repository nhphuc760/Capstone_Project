using Fusion;
using UnityEngine;
using UtilityAI.Core;
using Util.Core;

public class Trap : StructureBase, IInteractable
{

    NetworkObject target;

    [SerializeField] TrapBullet bullet;
    [Networked] NetworkBool IsTrapped { get; set; }
    [SerializeField] float speedTime = 1f;
    [SerializeField] InteractionData interactionData;
    public override void Spawned()
    {
        base.Spawned();
        var door = Runner.GetPlayerObject(Object.InputAuthority).GetBehaviour<NetworkPlayer>().Door;
        door.HealthDoor.OnTakeDamage += HealthDoor_OnTakeDamage;

    }

    private void HealthDoor_OnTakeDamage(NetworkObject obj)
    {
        target = obj;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if(target != null && !IsTrapped)
        {
            //Nếu target 
            if ((transform.position - target.transform.position).sqrMagnitude <= _structureStats.Get(StatsType.Range) && target.GetBehaviour<AIController>().GetCurrentState() == EnemyState.MoveToSafeZone)
            {
                if (Random.value <= _structureStats.Get(StatsType.Value))
                {
                    IsTrapped = true;
                    bullet.SetTarget(target.transform, speedTime);  
                    bullet.transform.parent = null;
                    return;
                }
                target = null;
            }                      
            
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        var door = Runner.GetPlayerObject(Object.InputAuthority).GetBehaviour<NetworkPlayer>().Door;
        door.HealthDoor.OnTakeDamage -= HealthDoor_OnTakeDamage;
    }

    public override string GetInfor()
    {
        return $"Percent: {_structureStats.Get(StatsType.Value)}\n" +
            $"Range: {_structureStats.Get(StatsType.Range)}";
    }

    public override void UpgradeLogic()
    {

    }    

    public void RequestStunTarget()
    {
        if (!HasStateAuthority || target == null) return;      
        Debug.Log("Trói chân enemy");
        target.GetComponent<IAffector>().AddModifier(new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = Runner.GetPlayerObject(Object.InputAuthority),
            IsTimed = true,
            Duration = TickTimer.CreateFromSeconds(Runner, 3f),
            StatType = StatsType.MoveSpeed,
            ModApplyType = ModApplyType.Override,
            Priority = 99,
            Value = 0
        });
        //Play animation stun
        var stategy = StructureManager.Ins.GetStrategyBuild(StructureDataSO._id, Object.InputAuthority);
        stategy.Destroy(Runner, Object);

    }


    public Transform InteractTransform => transform;

    public System.Action OnCompleted { get; set; }


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
