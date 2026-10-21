using Fusion;
using UnityEngine;

public class AccelerationTower : StructureBase
{
    EventBinding<BuildStategyEvent> onStructureChanged;
    [Networked] int idModifier { get; set; }



    public override void Spawned()
    {
        base.Spawned();
        onStructureChanged = new EventBinding<BuildStategyEvent> (OnStructureChanged);
        EventBus<BuildStategyEvent>.Register(onStructureChanged);
        idModifier = ModifierIDGenerator.Next(Runner);
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
        if(turrets != null && turrets.Count > 0)
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
            if(Runner.TryFindObject(args.ObjectId, out NetworkObject turret))
            {
                Debug.Log("Add modifier for turret");
                turret.GetComponent<IAffector>().AddModifier(CreateModifier());                
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

    }

    public override string GetInfor()
    {
        return $"IDMod: {idModifier}\n" +
            $"Speed: {_structureStats.Get(StatsType.Value)}";
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
            Value = _structureStats.Get(StatsType.Value),
            StatType = StatsType.AttackSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            IsTimed = false,
        };
    }
    
}
