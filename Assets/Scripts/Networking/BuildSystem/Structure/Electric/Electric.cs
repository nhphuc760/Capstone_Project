using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

public class Electric : StructureBase, IInteractable
{
    [Networked] TickTimer SpawnElectricSpeed { get; set; }
    [SerializeField] InteractionData interactionData;
    [SerializeField] ElectricMesh[] electricMeshes;
    [SerializeField] Transform[] spawnPoints;
    public override void Spawned()
    {
        base.Spawned();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();        
        if (SpawnElectricSpeed.ExpiredOrNotRunning(Runner))
        {
            SpawnElectricSpeed = TickTimer.CreateFromSeconds(Runner, 1/_structureStats.Get(StatsType.Speed));
            //Spawn Electric
            SpawnElectric();
        }
    }

    void SpawnElectric()
    {
        var player = Runner.GetPlayerObject(Object.InputAuthority).GetBehaviour<NetworkPlayer>();
        player.AddResource((int)_structureStats.Get(StatsType.Value), ResourceType.Electric);
        var ins = GetInActive();
        if (ins)
        {
            var spawnPoint = spawnPoints.RandomElement();
            ins.transform.localPosition = spawnPoint.localPosition;
            ins.gameObject.SetActive(true);
        }
    }

    ElectricMesh GetInActive()
    {
        foreach (var i in electricMeshes)
        {
            if (!i.gameObject.activeSelf)
            {
                return i;
            }
        }
        return null;
    }



    public override string GetInfor()
    {
        return $"Electric: {(int)_structureStats.Get(StatsType.Value)}/{1/_structureStats.Get(StatsType.Speed)}s";
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
