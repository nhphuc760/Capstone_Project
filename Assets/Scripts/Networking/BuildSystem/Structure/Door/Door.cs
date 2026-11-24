using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Game.Interaction;
using SilentCastle.MonsterAI;
using UnityEngine;

public class Door : StructureBase, ITakedamageable, IOwnable, IMonsterTarget
{

    [HideInInspector]
    [Networked]
    public Vector3 OutideOppositeDoor { get; private set; }

    [Networked]
    public NetworkBool HasInitialized { get; set; }

    public HealthComponent HealthDoor { get; private set; }

    [Networked]
    public NetworkBool IsOpen { get; set; }

    public int MaxSecurity = 10;
    [Networked] public int Security { get; set; } = 1;

    public NetworkId TargetId => Object.Id;

    public TargetKind Kind => TargetKind.Gate;

    public Vector3 Position => OutideOppositeDoor;

    public bool IsAlive => HealthDoor.IsAlive;

    public float Health01 => HealthDoor.HealthPercent;

    public float BodyRadius => 0f;

    public bool IsTargetable => IsAlive && isActiveAndEnabled;




    public event Action<bool, PlayerRef> OnDoorToggle;


    public void SetOutSideOppositeDoor(Vector3 pos)
    {
        if (HasStateAuthority)
        {
            OutideOppositeDoor = pos;
            Vector3 directionDoor = (OutideOppositeDoor - transform.position).normalized;
            RPC_SetDoorRotation(Quaternion.LookRotation(directionDoor));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetDoorRotation(Quaternion rotation)
    {
        GetBehaviour<NetworkTransform>().Teleport(rotation: rotation);
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
        HasInitialized = false;
        if (WorldContext.Instance)
            WorldContext.Instance.Register(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        HealthDoor.OnDeath -= HealthDoor_OnDeath;
        if (WorldContext.Instance)
            WorldContext.Instance.Unregister(this);

        if (HasStateAuthority && NavMeshBakeManager.Ins != null)
            NavMeshBakeManager.Ins.RequestBake();

        //GameOver on Object.InputAuthority

    }


    private void HealthDoor_OnDeath(NetworkObject obj)
    {
        EndGame();
    }


    public void EndGame()
    {
        // Chỉ StateAuthority mới được quyết định despawn
        if (Object.HasStateAuthority == false)
            return;

        PlayerRef owner = Object.InputAuthority;

        // Gửi RPC cho tất cả client (đặc biệt là owner) để xử lý visual + UI
        RPC_OnDoorDeath(owner, transform.position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDoorDeath(PlayerRef owner, Vector3 doorPosition)
    {
        // 1. Nếu đây là local player sở hữu Door → Zoom camera về vị trí Door
        if (Runner.LocalPlayer == owner)
        {
            HandleLocalPlayerEliminated(doorPosition);
        }
    }

    private async void HandleLocalPlayerEliminated(Vector3 doorPosition)
    {
        // Lấy camera controller của player
        var playerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (playerObj == null) return;

        var camCtrl = playerObj.GetComponentInChildren<PlayerCameraController>();
        if (camCtrl != null)
        {
            // Zoom view về vị trí Door
            camCtrl.MoveToView(doorPosition, 2f);
        }

        // Đợi camera zoom xong
        await UniTask.Delay(TimeSpan.FromSeconds(2f + 0.1f));

        // 2. Báo GameManager xử lý despawn + UI
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerEliminated(Runner.LocalPlayer);
        }
    }
    public override void UpgradeLogic()
    {
        if (HasStateAuthority)
            Security++;
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
        if (value)
        {
            HasInitialized = true;
        }
    }

    public void TakeDamage(float amount, NetworkObject attacker)
    {        
        HealthDoor?.TakeDamage(amount, attacker);       
    }

    public override string GetInfor()
    {
        string s = "";

        s +=  $"Máu: {HealthDoor.CurrentHealth}/{HealthDoor.MaxHealth} \n";
        s += $"Cấp: {Level} \n";
        s += $"Mức Bảo mật: {Security}";
        return s ;  
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RequestTriggerDoor(bool open, PlayerRef player = default)
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

        OnDoorToggle?.Invoke(open, player);
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

    public void ReceiveDamage(float amount, MonsterBrain attacker)
    {
        HealthDoor.TakeDamage(amount, attacker.Object);
    }
   


  
}




