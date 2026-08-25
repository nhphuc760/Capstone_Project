using System;
using Fusion;
using System.Collections.Generic;
using Fusion.Addons.SimpleKCC;
using Fusion.LagCompensation;
using UnityEngine;
using System.Linq;


public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local { get; private set; }
    public HealthComponent Health { get; private set; }
    public StaminaComponent Stamina { get; private set; }

    [SerializeField] SimpleKCC controller;
    [SerializeField] Camera cameraView;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float moveSpeed = 5f;

    [Networked] NetworkButtons previousInput { get; set; }
    // Inventory inventory;

    //Resource Exploitation
    [SerializeField] float gatherRanged = 1.5f;
    [SerializeField] float mineInterval = 1f;
    [Networked] public ToolType equipTool { get; set; }
    [Networked] TickTimer mineIntervalTimer { get; set; }

    readonly List<LagCompensatedHit> hits = new List<LagCompensatedHit>(5);

    [Networked] public int wood { get; set; }
    [Networked] public int copperOre { get; set; }
    [Networked] public int ironOre { get; set; }
    [Networked] public int goldOre { get; set; }
    public int pros { get; set; }

    public event Action<ResourceType, int> OnResourceGathered;
    public event Action<string> OnGatheredFailed;


    public void Awake()
    {
        Health = GetComponent<HealthComponent>();
        Stamina = GetComponent<StaminaComponent>();
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
        equipTool = ToolType.Axe;

    }


    private void Update()
    {
        //if (!Object.HasInputAuthority) return;
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("SetTool update");
            equipTool = ToolType.Axe;
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 inputDirection = new Vector3(data.moveDirection.x, 0f, data.moveDirection.y);
            Vector3 moveDirection = inputDirection.normalized * moveSpeed;
            float jumpImpluse = 0f;
            if (data.button.WasPressed(previousInput, ButtonType.Jump) && controller.IsGrounded)
            {
                jumpImpluse = jumpForce;
            }
            if (data.button.WasPressed(previousInput, ButtonType.EquipTool))
            {
                Debug.Log("SetTool FixedUpdate");
                if (Object.HasStateAuthority)
                {
                    wood = 50;
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
    }


    public bool TryGather(ResourceNode nodeRes)
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
            int hitsCount = Runner.LagCompensation.OverlapBox(center, extents, Quaternion.identity, Object.InputAuthority, hits, 1 << 9, HitOptions.IncludePhysX, true);
            if (hitsCount == 0) return;
            HashSet<ResourceNode> test = new HashSet<ResourceNode>();
            test = hits.Select(x => x.Collider.GetComponentInParent<ResourceNode>()).ToHashSet();
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
                wood += amount;
                break;
            case ResourceType.IRON:
                ironOre += amount;
                break;
            case ResourceType.COPPER:
                copperOre += amount;
                break;
            case ResourceType.GOLD:
                goldOre += amount;
                break;
        }

    }
}
