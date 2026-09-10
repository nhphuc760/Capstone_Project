using System;
using Fusion;
using System.Collections.Generic;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using System.Linq;


public struct NetworkResourcePlayer : INetworkStruct
{
    public int Wood;
    public int IronOre;
    public int CopperOre;
    public int GoldOre;
}

public class NetworkPlayer : NetworkBehaviour, IAffector
{
    public static NetworkPlayer Local { get; private set; }

    [SerializeField] StatsBase baseStats;

    public HealthComponent Health { get; private set; }
    public StaminaComponent Stamina { get; private set; }

    public Stats Stats { get; private set; }


    [SerializeField] SimpleKCC controller;
    [SerializeField] Camera cameraView;
    [SerializeField] float jumpForce = 8f;

    [Networked] NetworkButtons previousInput { get; set; }
    public NetworkInventory inventory { get; private set; }

    //Resource Exploitation
    [SerializeField] float gatherRanged = 1.5f;
    [SerializeField] float mineInterval = 1f;
    [Networked] public ToolType equipTool { get; set; }
    [Networked] TickTimer mineIntervalTimer { get; set; }

    readonly List<LagCompensatedHit> resourcesHit = new List<LagCompensatedHit>(5);

    [Networked]
    public ref NetworkResourcePlayer resourcePlayer => ref MakeRef<NetworkResourcePlayer>();

    public event Action<ResourceType, int> OnResourceGathered;
    public event Action<string> OnGatheredFailed;
    





    public void Awake()
    {
        Health = GetComponent<HealthComponent>();
        Stamina = GetComponent<StaminaComponent>();
        inventory = GetComponent<NetworkInventory>();
    }


    public override void Spawned()
    {
        if (Runner.LocalPlayer == Object.InputAuthority && Local == null)
        {
            Local = this;
        }
        if (!Object.HasInputAuthority)
        {
            cameraView.gameObject.SetActive(false);
        }
        Debug.Log("HasStateAuthority: " + Object.HasStateAuthority);
        controller.SetGravity(Physics.gravity.y * 2f);
        gameObject.name = Object.InputAuthority.ToString();
        Stats = new Stats(baseStats, Resources.LoadAll<ModifierDatabaseSO>("ScriptableObjects").First());
        Health ??= GetComponent<HealthComponent>();
        Stamina ??= GetComponent<StaminaComponent>();
        Health.Initialize(Stats);
        Stamina.Initialize(Stats);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && HasInputAuthority)
        {
            RPC_RequestAddModifier(new NetworkString<_8> { Value = "SP_FL_10" });
        }

    }

    public override void FixedUpdateNetwork()
    {

        Stats.Tick(Runner.DeltaTime);
        if (GetInput(out NetworkInputData data))
        {
            Vector3 inputDirection = new Vector3(data.moveDirection.x, 0f, data.moveDirection.y);
            Vector3 moveDirection = inputDirection.normalized * Stats.Get(StatsType.Speed);
            float jumpImpluse = 0f;
            if (data.button.WasPressed(previousInput, ButtonType.Jump) && controller.IsGrounded)
            {
                jumpImpluse = jumpForce;
            }
            if (data.button.WasPressed(previousInput, ButtonType.EquipTool))
            {
                if (Object.HasStateAuthority)
                {
                    equipTool = ToolType.Axe;
                }
                //RPC_TestSet();
            }
            if (data.button.IsSet(ButtonType.Interact))
            {
                Debug.Log("EquipTool: " + equipTool.ToString());
                if (equipTool == ToolType.None) return;

                TryMineResource();
            }
            previousInput = data.button;
            controller.Move(moveDirection, jumpImpluse);
        }

        if (transform.position.y <= -10f)
        {
            controller.SetPosition(Vector3.one);
        }
    }


    public bool TryGather(ResourceNode nodeRes, object source = null)
    {
        Debug.Log("TryGather");
        if (!Health.IsAlive)
        {
            Debug.Log("Người chơi đã chết");
            OnGatheredFailed?.Invoke("Người chơi đã chết");
            return false;
        }

        if (nodeRes == null || nodeRes.IsDepleted)
        {
            Debug.Log("Tài nguyên đã cạn");
            OnGatheredFailed?.Invoke("Tài nguyên đã cạn");
            return false;
        }

        float distance = Vector3.Distance(nodeRes.transform.position, transform.position);
        if (distance > gatherRanged)
        {
            Debug.Log("Đứng quá xa điểm tài nguyên");
            OnGatheredFailed?.Invoke("Đứng quá xa điểm tài nguyên");
            return false;
        }

        if (nodeRes.RequiredTool != ToolType.None && equipTool != nodeRes.RequiredTool)
        {
            Debug.Log("Công cụ khai thác không phù hợp");
            OnGatheredFailed?.Invoke("Công cụ khai thác không phù hợp");
            return false;
        }

        Vector3 dirToNode =( nodeRes.transform.position - transform.position).normalized;
        if (!(Vector3.Dot(dirToNode, transform.forward) > 0))
        {
            Debug.Log("Player không úp mặt vào node");
            return false;
        }

        if (!Stamina.TryConsume(nodeRes.StaminaCostPerGather))
        {
            Debug.Log("Hết Stamina, cần nghỉ hoặc vật phẩm hồi phục");
            OnGatheredFailed?.Invoke("Hết Stamina, cần nghỉ hoặc vật phẩm hồi phục");
            return false;
        }
        AddResource(nodeRes.Extract(), nodeRes.ResourceType);
        return true;
    }


    void TryMineResource()
    {
        if (mineIntervalTimer.ExpiredOrNotRunning(Runner))
        {
            mineIntervalTimer = TickTimer.CreateFromSeconds(Runner, mineInterval);
            Vector3 center = transform.position + Vector3.up * .5f;
            Vector3 extents = new Vector3(gatherRanged, 1f, gatherRanged);
            DrawLog.DrawCube(center, extents, Color.red);
            int hitsCount = Runner.LagCompensation.OverlapBox(center, extents, Quaternion.identity, Object.InputAuthority, resourcesHit, 1 << 9, HitOptions.IncludePhysX, true);
            if (hitsCount == 0) return;
            HashSet<ResourceNode> test = new HashSet<ResourceNode>();
            test = resourcesHit.Select(x => x.Collider.GetComponentInParent<ResourceNode>()).ToHashSet();
            foreach (var i in test)
            {
                Debug.Log("Collider Hit: " + i.transform.name);              
                Debug.Log($"Node != null ? {i != null}");
                TryGather(i);
            }
        }
    }


    void AddResource(int amount, ResourceType type)
    {
        Debug.Log($"AddResource called on {Runner.LocalPlayer} in {Object.InputAuthority}");
        if (!Object.HasStateAuthority) return;
        switch (type)
        {
            case ResourceType.WOOD:
                resourcePlayer.Wood += amount;
                break;
            case ResourceType.IRON:
                resourcePlayer.IronOre += amount;
                break;
            case ResourceType.COPPER:
                resourcePlayer.CopperOre += amount;
                break;
            case ResourceType.GOLD:
                resourcePlayer.GoldOre += amount;
                break;
        }

    } 

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestAddModifier(NetworkString<_8> modID, NetworkId sourceID = default)
    {
        NetworkObject source = null;
        if (sourceID != default)
        {
            source = Runner.FindObject(sourceID);
        }       
        
        AddModifier(modID.Value, source);
    }

    public void AddModifier(string idMod, NetworkObject source = null)
    {        
        Stats.AddModifierById(idMod, source);
    }  
}
