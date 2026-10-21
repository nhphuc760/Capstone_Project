using Fusion;
using UnityEngine;
using UtilityAI.Core;
using Util.Core;

public class Trap : StructureBase
{

    NetworkObject target;

    [Networked] NetworkBool IsTrapped { get; set; }

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

        if(target != null && HasStateAuthority && !IsTrapped)
        {
            //Nếu target 
            if ((transform.position - target.transform.position).sqrMagnitude <= _structureStats.Get(StatsType.Range) && target.GetBehaviour<AIController>().GetCurrentState() == EnemyState.MoveToSafeZone)
            {
                if (Random.value <= _structureStats.Get(StatsType.Value))
                {
                    IsTrapped = true;
                    Utils.DelayCall(1f, () => 
                    {
                        if(target == null)
                        {
                            return;
                        }
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

                        var stategy = StructureManager.Ins.GetStrategyBuild(StructureDataSO._id, Object.InputAuthority);
                        stategy.Destroy(Runner, Object);
                    }).Forget();
                    //Thêm modifier cấm di chuyển
                    
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
}
