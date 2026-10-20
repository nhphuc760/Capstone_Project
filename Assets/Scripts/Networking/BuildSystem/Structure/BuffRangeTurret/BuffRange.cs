using Fusion;

public class BuffRange : StructureBase
{
    [Networked] int idModifier { get; set; }
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

}
