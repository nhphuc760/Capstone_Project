using Fusion;
using UnityEngine;

public class BuffRange : StructureBase, IInteractable
{
    [Networked] int idModifier { get; set; }
    [SerializeField] InteractionData interactionData;
    EventBinding<BuildStategyEvent> onStructureChanged;

    public override void Spawned()
    {
        base.Spawned();
        idModifier = ModifierIDGenerator.Next(Runner);
        onStructureChanged = new EventBinding<BuildStategyEvent>(OnStructureChanged);
        EventBus<BuildStategyEvent>.Register(onStructureChanged);
        var turrets = StructureManager.Ins?.WithCategory(StructureCategory.Attack).WithType(StructureType.Turret).WithPlayerRef(Object.InputAuthority).Get();

        foreach (var i in turrets.Values)
        {
            i.AddModifier(CreateModifier());
        }
    }


    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        var turrets = StructureManager.Ins?.WithCategory(StructureCategory.Attack).WithType(StructureType.Turret).WithPlayerRef(Object.InputAuthority).Get();
        if (turrets != null && turrets.Count > 0)
        {
            foreach (var i in turrets?.Values)
            {
                i.RemoveModifier(idModifier);
            }
        }
        EventBus<BuildStategyEvent>.Deregister(onStructureChanged);
    }

    void OnStructureChanged(BuildStategyEvent args)
    {
        if (args.BuildType == BuildStategyEvent.BuildEventType.Build && args.StructureType == StructureType.Turret)
        {
            if (Runner.TryFindObject(args.ObjectId, out NetworkObject turret))
            {
                turret.GetComponent<IAffector>().AddModifier(CreateModifier());
            }
        }
    }

    public override string GetInfor()
    {
        return $"IDMod: {idModifier}\n" +
             $"Range: {_structureStats.Get(StatsType.Value)}";
    }

    public override void UpgradeLogic()
    {
        var turrets = StructureManager.Ins?.WithCategory(StructureCategory.Attack).WithType(StructureType.Turret).WithPlayerRef(Object.InputAuthority).Get();
        foreach (var i in turrets.Values)
        {            
            i.AddModifier(CreateModifier());
        }
    }   


    Modifier CreateModifier()
    {
        return new Modifier
        {
            ModID = idModifier,
            Source = Object,
            ModApplyType = ModApplyType.PercentAdd,
            StatType = StatsType.Range,
            IsTimed = false,
            Value = _structureStats.Get(StatsType.Value),
        };

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
