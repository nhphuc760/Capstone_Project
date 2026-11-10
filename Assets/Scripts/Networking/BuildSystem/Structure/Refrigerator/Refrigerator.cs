using Fusion;
using UnityEngine;
public class Refrigerator : StructureBase, IInteractable
{
    [Networked] int idModifier { get; set; }
    [SerializeField] InteractionData interactionData;

    public override void Spawned()
    {
        base.Spawned();
        idModifier = ModifierIDGenerator.Next(Runner);
        Door doorPlayer = Runner.GetPlayerObject(Object.InputAuthority).GetComponent<NetworkPlayer>().Door;
        doorPlayer.HealthDoor.OnTakeDamage += HealthDoor_OnTakeDamage;        
    }

    private void HealthDoor_OnTakeDamage(NetworkObject obj)
    {
        obj.GetComponent<IAffector>().AddModifier(new Modifier
        {
            ModID = idModifier,
            Source = Object,
            StatType = StatsType.AttackSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            IsTimed = true,
            Value = -_structureStats.Get(StatsType.Value),
            Duration = TickTimer.CreateFromSeconds(Runner, 2f),
        });
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        Door doorPlayer = Runner.GetPlayerObject(Object.InputAuthority).GetComponent<NetworkPlayer>().Door;
        if(doorPlayer)
            doorPlayer.HealthDoor.OnTakeDamage -= HealthDoor_OnTakeDamage;
    }

    public override string GetInfor()
    {
        return $"Giảm tốc độ đánh của quái lên cửa {_structureStats.Get(StatsType.Value)}%";
    }

    public override void UpgradeLogic()
    {
        
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
