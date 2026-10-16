using System;
using Fusion;
using System.Collections.Generic;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;


public struct NetworkResourcePlayer : INetworkStruct
{
    public int Wood;
    public int IronOre;
    public int CopperOre;
    public int GoldOre;
}

public class NetworkPlayer : NetworkBehaviour, IAffector, ITakedamageable
{
    public static NetworkPlayer Local { get; private set; }
    public HealthComponent Health { get; private set; }
    public StaminaComponent Stamina { get; private set; }

    [SerializeField] GameObject CanvasWorldSpace;
    [SerializeField] GameObject CanvasOverlay;
    [SerializeField] TooltipUI TooltipUI;
    [SerializeField] StructureUI StructureUI;
    public Stats _stats { get; private set; }


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

    [Networked, OnChangedRender(nameof(OnResourceChanged))]
    public ref NetworkResourcePlayer resourcePlayer => ref MakeRef<NetworkResourcePlayer>();
    public event Action<NetworkResourcePlayer> OnResouceChange;






    private void OnResourceChanged()
    {
        OnResouceChange?.Invoke(resourcePlayer);
    }

    public event Action<ResourceType, int> OnResourceGathered;
    public event Action<string> OnGatheredFailed;

    [SerializeField] NetworkPrefabRef doorPrefab;

    Door doorPlayer;
    public Door Door => doorPlayer;

    public override void Spawned()
    {
        if (Runner.LocalPlayer == Object.InputAuthority && Local == null)
        {
            Local = this;
        }
        if (!Object.HasInputAuthority)
        {
            cameraView.gameObject.SetActive(false);
            CanvasOverlay.SetActive(false);
        }
        Debug.Log("HasStateAuthority: " + Object.HasStateAuthority);
        controller.SetGravity(Physics.gravity.y * 2f);
        gameObject.name = Object.InputAuthority.ToString();
        inventory ??= GetComponent<NetworkInventory>();
        Health ??= GetComponent<HealthComponent>();
        Stamina ??= GetComponent<StaminaComponent>();
        _stats ??= GetComponent<Stats>();
        Health.Initialize(_stats);
        Stamina.Initialize(_stats);
        if (HasInputAuthority)
        {

            //Subcribe Inventory Event
            cameraView.tag = "MainCamera";
            CanvasWorldSpace.gameObject.SetActive(false);
            var inventoryUI = GetComponentInChildren<InventoryUI>(includeInactive: true);
            Debug.Log(inventoryUI.gameObject.name);
            Debug.Log("InventoryUI: " + inventoryUI == null);
            if (inventoryUI != null)
            {
                Debug.Log("Register event InventoryChanged");
                inventoryUI.SetInventory(inventory);
                inventory.OnInventoryChanged += inventoryUI.OnInventoryChanged;
            }


            //Subcribe RadialMenu event
            var radialmenu = GetComponentInChildren<RadialMenu>(includeInactive: true);
            if (radialmenu != null)
            {
                radialmenu.onSelectIndex += OnRadialMenuSelected;
            }
        }

        if (HasStateAuthority)
        {
            doorPlayer = Runner.Spawn(doorPrefab, inputAuthority: Object.InputAuthority).GetBehaviour<Door>();
            doorPlayer.RPC_SetActiveNetworked(false);
        }

    }


    private void Update()
    {       
        if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
        {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.TryGetComponent<StructureBase>(out var context))
                {
                    TooltipUI.Show(context.GetInfor(), Input.mousePosition, context.gameObject);
                }
                else
                {
                    TooltipUI.Hide();
                }
            }
        }


        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.TryGetComponent<StructureBase>(out var context))
                {
                    StructureUI.Show(context);
                }
                else
                {
                    StructureUI.Hide();
                }
            }
        }


        

    }    

    public override void FixedUpdateNetwork()
    {

        _stats.Tick(Runner.DeltaTime);
        if (GetInput(out NetworkInputData data))
        {
            Vector3 inputDirection = new Vector3(data.moveDirection.x, 0f, data.moveDirection.y);
            Vector3 moveDirection = inputDirection.normalized * _stats.Get(StatsType.MoveSpeed);
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
            if (data.button.WasPressed(previousInput, ButtonType.TestAddModify))
            {
                Modifier modSpeed = new Modifier
                {
                    ModApplyType = ModApplyType.Flat,
                    StatType = StatsType.MoveSpeed,
                    Value = 10,
                    Source = Object.Id,
                    Duration = TickTimer.CreateFromSeconds(Runner, 5f),
                    IsTimed = true,
                };
                AddModifier(modSpeed);
            }
            previousInput = data.button;
            controller.Move(moveDirection, jumpImpluse);
        }

        if (transform.position.y <= -10f)
        {
            controller.SetPosition(Vector3.one);
        }
    }

    private void OnDestroy()
    {
        if (HasInputAuthority)
        {
            if (inventory)
            {
                var InventoryUI = GetComponentInChildren<InventoryUI>();

                if (InventoryUI != null)
                {
                    inventory.OnInventoryChanged -= InventoryUI.OnInventoryChanged;
                }
            }
            var radialMenu = GetComponentInChildren<RadialMenu>(includeInactive: true);
            if (radialMenu != null)
            {
                radialMenu.onSelectIndex -= OnRadialMenuSelected;
            }

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

        Vector3 dirToNode = (nodeRes.transform.position - transform.position).normalized;
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

    private void OnTriggerEnter(Collider other)
    {
      
    }

    private void OnCollisionEnter(Collision collision)
    {
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
  
    public void ComsumpResource(NetworkResourcePlayer resource)
    {
        if (!HasStateAuthority) return;
        this.resourcePlayer.Wood -= resource.Wood;
        this.resourcePlayer.IronOre -= resource.IronOre;
        this.resourcePlayer.CopperOre -= resource.CopperOre;    
        this.resourcePlayer.GoldOre -= resource.GoldOre;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestUseTool(ToolType toolUse)
    {
        equipTool = toolUse;
    }

    void OnRadialMenuSelected(int index)
    {
        switch (index)
        {
            case 0:
                RPC_RequestUseTool(ToolType.Screwdriver);
                break;
            case 1:
                RPC_RequestUseTool(ToolType.PickAxe);
                break;
            case 2:
                RPC_RequestUseTool(ToolType.None);
                break;
            case 3:
                RPC_RequestUseTool(ToolType.Axe);
                break;
        }
    }

    public void TakeDamage(float amount, NetworkObject attacker)
    {
        Health?.TakeDamage(amount, attacker);
    }

    public void AddModifier(Modifier mod)
    {
        if (HasStateAuthority)
        {
            mod.ModID = ModifierIDGenerator.Next(Runner);
            _stats.AddModifier(mod);
        }
    }
}
