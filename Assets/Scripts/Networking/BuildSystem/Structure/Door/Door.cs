using Fusion;
using Game.Interaction;
using UnityEngine;

public class Door : StructureBase, ITakedamageable, IOwnable
{

    [HideInInspector]
    [Networked]
    public Vector3 OutideOppositeDoor { get; private set; }
    public HealthComponent HealthDoor { get; private set; }

    [Networked]
    public NetworkBool IsOpen { get; set; }

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


    private void Awake()
    {
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RequestTriggerDoor(bool open)
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


   
  
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(OutideOppositeDoor, Vector3.one);
    }

    public bool IsOwnedBy(PlayerRef player)
    {
        return player == Object.InputAuthority;
    }      

}




