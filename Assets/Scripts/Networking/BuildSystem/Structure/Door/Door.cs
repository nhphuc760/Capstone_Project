using Fusion;
using UnityEngine;

public class Door : StructureBase, ITakedamageable, IInteractable
{

    [Networked]
    public Vector3 OutideOppositeDoor { get; private set; }
    public HealthComponent HealthDoor { get; private set; }

    [Networked]
    public NetworkBool IsOpen { get; set; }

    [SerializeField] InteractionData InteractionData;
    public Transform InteractTransform => transform;

    public void SetOutSideOppositeDoor(Vector3 pos)
    {
        if (HasStateAuthority)
        {
            OutideOppositeDoor = pos;
            Vector3 directionDoor = (OutideOppositeDoor - transform.position).normalized;        
            GetBehaviour<NetworkTransform>().Teleport(rotation: Quaternion.LookRotation(directionDoor));
        }      
        //Quaternion.L
    }

    public override void Spawned()
    {
        base.Spawned();
        _structureStats ??= GetComponent<Stats>();
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]

    public void RPC_SetActiveNetworked(bool value)
    {        
       gameObject.SetActive(value);
    }

    public void TakeDamage(float amount, NetworkObject attacker)
    {
       HealthDoor?.TakeDamage(amount, attacker);
    }

    public override string GetInfor()
    {
        return $"HP: {HealthDoor.CurrentHealth}/{HealthDoor.MaxHealth}";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_RequestTriggerDoor(bool open)
    {
        if (open)
        {
            //Mở             
            GetComponentInChildren<Animator>().Play("Open");
        }
        else
        {
            //đóng
            GetComponentInChildren<Animator>().Play("Close");
        }

        IsOpen = open;

    }

    public InteractionData Data(NetworkPlayer interactor) => InteractionData;

    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
       return new InteractionResult (true);
    }

    public void OnInteract(InteractionContext ctx)
    {
        if(ctx.Phase == InteractPhase.Press)
            RPC_RequestTriggerDoor(!IsOpen);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(OutideOppositeDoor, Vector3.one);
    }

}

