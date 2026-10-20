using Fusion;
using UnityEngine;

public class Electric : StructureBase
{
    [Networked] TickTimer SpawnElectricSpeed { get; set; }

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
            var player = Runner.GetPlayerObject(Object.InputAuthority).GetBehaviour<NetworkPlayer>();
            player.AddResource((int)_structureStats.Get(StatsType.Value), ResourceType.Electric);
        }
    }

    public override string GetInfor()
    {
        return $"Electric: {(int)_structureStats.Get(StatsType.Value)}/{1/_structureStats.Get(StatsType.Speed)}s";
    }

    public override void UpgradeLogic()
    {

    }

    
}
