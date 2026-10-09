using Fusion;
using UnityEngine;

public class Door : StructureBase, ITakedamageable
{

    [Networked]
    public Vector3 OutideOppositeDoor { get; private set; }
    [SerializeField]
    HealthComponent HealthDoor;

    
    public void SetOutSideOppositeDoor(Vector3 pos)
    {
        if (HasStateAuthority)
        {
            OutideOppositeDoor = pos;
        }
    }

    public override void Spawned()
    {
        base.Spawned();
         HealthDoor ??= GetComponent<HealthComponent>();
        HealthDoor.Initialize(_structureStats);
        HealthDoor.OnDeath += HealthDoor_OnDeath;
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        HealthDoor.OnDeath -= HealthDoor_OnDeath;
    }

    private void HealthDoor_OnDeath(NetworkObject obj)
    {
        Debug.Log("Destroy Door");
        StructureManager.Ins?.DestroyStructure(Object);
    }

    public override void Operation()
    {

    }

    public override void UpgradeLogic()
    {
        Debug.Log("Door upgrade");
    }

    


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUpgrade()
    {

        var canUpgrade = CanUpgrade();

        if (canUpgrade.Success)
        {
            Upgrade();
            Debug.Log("");
        }
        else
        {
            Debug.Log(canUpgrade.Message);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void RPC_SetActiveNetworked(bool value)
    {        
       gameObject.SetActive(value);
    }

    public void TakeDamage(float amount, NetworkObject attacker)
    {
       HealthDoor?.TakeDamage(amount, attacker);
    }
}
