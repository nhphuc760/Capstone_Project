using System;
using System.Collections.Generic;
using Fusion;
using TriInspector;
using UnityEngine;

public class Door : StructureBase, ITakedamageable, IInteractable
{

    [HideInInspector]
    [Networked]
    public Vector3 OutideOppositeDoor { get; private set; }
    public HealthComponent HealthDoor { get; private set; }

    [Networked]
    public NetworkBool IsOpen { get; set; }

    [SerializeField] InteractionData NormalInteractionData;
    [SerializeField] InteractionData DismantleInteractionData;
    [SerializeField] InteractProcess processor;

    List<IInteractable> _interactables = new List<IInteractable>();
    IInteractable _currentInteractStategy;


    public Transform InteractTransform => transform;

    public Action OnCompleted { get; set; }

    public IProcessor Processor => processor;

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
        InitializeInteractables();
    }


    void InitializeInteractables()
    {
        // Normal interaction strategy
        _interactables.Add(new NormalInteract(transform, NormalInteractionData, this));
        var dismantle = new DismantleInteract(transform, DismantleInteractionData, this);
        dismantle.OnCompleted += () => OnCompleted?.Invoke();
        _interactables.Add(dismantle);
    }
    IInteractable GetInteractStategy(NetworkPlayer interactor)
    {
        if (interactor.equipTool == ToolType.Screwdriver)
        {
            return _interactables.Find(i => i is DismantleInteract);
        }
        else
        {
            return _interactables.Find(i => i is NormalInteract);
        }
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

    public InteractionData Data(NetworkPlayer interactor) => _currentInteractStategy.Data(interactor);

    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
        _currentInteractStategy = GetInteractStategy(interactor);
        return _currentInteractStategy.CanInteract(interactor);
        //return new InteractionResult (true);
    }

    public void OnInteract(InteractionContext ctx)
    {
        _currentInteractStategy.OnInteract(ctx);        
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(OutideOppositeDoor, Vector3.one);
    }

    public class NormalInteract : IInteractable
    {

        Transform Transform;
        InteractionData InteractData;
        Door door;


        public NormalInteract(Transform transform, InteractionData data, Door door)
        {
            this.Transform = transform;
            this.InteractData = data;
            this.door = door;
        }

        public Transform InteractTransform => Transform;

        public Action OnCompleted { get; set; }


        public InteractionResult CanInteract(NetworkPlayer interactor)
        {

            return new InteractionResult(success: interactor.Object.InputAuthority == door.Object.InputAuthority);            
        }

        public InteractionData Data(NetworkPlayer interactor) => InteractData;
        public void OnInteract(InteractionContext ctx)
        {
           if(ctx.Phase == InteractPhase.Press)
            {
                door.RPC_RequestTriggerDoor(!door.IsOpen);
            }
        }
    }


    public class DismantleInteract : IInteractable
    {
        Transform Transform;
        InteractionData InteractData;
        Door door;

        public DismantleInteract(Transform transform, InteractionData data, Door door)
        {
            this.Transform = transform;
            this.InteractData = data;
            this.door = door;
        }
        public Transform InteractTransform => Transform;

        public Action OnCompleted { get; set; }


        public InteractionResult CanInteract(NetworkPlayer interactor)
        {
            return new InteractionResult(!door.IsOpen);
        }
        public InteractionData Data(NetworkPlayer interactor) => InteractData;
        public void OnInteract(InteractionContext ctx)
        {
            if (ctx.Phase == InteractPhase.Hold)
            {
                door.processor.UpdateProcess(ctx.HoldTime / InteractData.HoldTime);
                if (ctx.HoldTime >= InteractData.HoldTime)
                {
                    door.RPC_RequestTriggerDoor(true);
                    OnCompleted?.Invoke();
                }
            }
        }
    }




}




