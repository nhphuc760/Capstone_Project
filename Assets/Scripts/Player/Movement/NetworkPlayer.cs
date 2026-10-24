using System;
using Fusion;
using System.Collections.Generic;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Text;
using Newtonsoft.Json;


public struct NetworkResourcePlayer : INetworkStruct
{
    public int Wood;
    public int Stone;
    public int GoldOre;
    public int Electric;
}

[Serializable]
public class GatherResult
{
    public enum FailReason
    {
        None,
        PlayerDead,//Player chết
        Depleted,//Tài nguyên cạn
        FarDistance,
        InvalidTool,
        Exhausted // Kiệt sức- hết stamina
    }
    public FailReason Reason;
    public bool Success => Reason == FailReason.None;
    public string Message;
    public static GatherResult OK(string Message = null) => new GatherResult { Message = Message };
    public static GatherResult FAIL(FailReason reason, string Message = null) => new GatherResult { Reason = reason, Message = Message };
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


    [SerializeField] SuggestTarget suggestTarget;

    readonly List<LagCompensatedHit> resourcesHit = new List<LagCompensatedHit>(5);

    [Networked, OnChangedRender(nameof(OnResourceChanged))]
    public ref NetworkResourcePlayer resourcePlayer => ref MakeRef<NetworkResourcePlayer>();
    public event Action<NetworkResourcePlayer> OnResouceChange;

    EventBinding<ReliableReceiveData> relialeEvent;


    private void OnResourceChanged()
    {
        OnResouceChange?.Invoke(resourcePlayer);
    }

    public event Action<ResourceType, int> OnResourceGathered;

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

            //Suggest Target
            suggestTarget ??= GetComponent<SuggestTarget>();
            suggestTarget.enabled = true;
            //Subcribe RadialMenu event
            var radialmenu = GetComponentInChildren<RadialMenu>(includeInactive: true);
            if (radialmenu != null)
            {
                radialmenu.onSelectIndex += OnRadialMenuSelected;
            }
            relialeEvent = new EventBinding<ReliableReceiveData>(OnReliableReceiveData);
            EventBus<ReliableReceiveData>.Register(relialeEvent);
        }

        if (HasStateAuthority)
        {
            doorPlayer = Runner.Spawn(doorPrefab, inputAuthority: Object.InputAuthority).GetBehaviour<Door>();
            doorPlayer.RPC_SetActiveNetworked(false);
        }

    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            EventBus<ReliableReceiveData>.Deregister(relialeEvent);
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
                if (hit.collider.transform.root.TryGetComponent<ITooltipContext>(out var context))
                {
                    TooltipUI.Show(context.GetContent(), Input.mousePosition, hit.collider.gameObject);
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
                if (hit.collider.TryGetComponent<StructureBase>(out var context) && context.Object.InputAuthority == Object.InputAuthority)
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
                if (!HasStateAuthority) return;
                Modifier modSpeed = new Modifier
                {
                    ModID = ModifierIDGenerator.Next(Runner),
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

    public GatherResult TryGather(ResourceNode nodeRes, object source = null)
    {
        Debug.Log("TryGather");
        if (!Health.IsAlive)
        {
            return GatherResult.FAIL(reason: GatherResult.FailReason.PlayerDead, "Ngỏm củ tỏi rồi!");
        }

        if (nodeRes == null || nodeRes.IsDepleted)
        {
            return GatherResult.FAIL(GatherResult.FailReason.Depleted, "Tài nguyên đã cạn kiệt");
        }        

        if (nodeRes.RequiredTool != ToolType.None && equipTool != nodeRes.RequiredTool)
        {
            return GatherResult.FAIL(GatherResult.FailReason.InvalidTool, "Công cụ khai thác không phù hợp");
        }

        //Vector3 dirToNode = (nodeRes.transform.position - transform.position).normalized;
        //if (!(Vector3.Dot(dirToNode, transform.forward) > 0))
        //{
        //    Debug.Log("Player không úp mặt vào node");
        //    return false;
        //}

        if (!Stamina.TryConsume(nodeRes.StaminaCostPerGather))
        {
            return GatherResult.FAIL(GatherResult.FailReason.Exhausted, "Kiệt sức !");
        }
        int resourceExtract = nodeRes.Extract();
        AddResource(nodeRes.Extract(), nodeRes.ResourceType);
        return GatherResult.OK($"Đã thu thập <Sprite_{nodeRes.ResourceType.ToString()}> x{resourceExtract}");
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {

    }

    void TryMineResource()
    {
        if (!HasInputAuthority) return;
        if (mineIntervalTimer.ExpiredOrNotRunning(Runner) && suggestTarget.CurrentTarget != null)
        {
            RPC_RequestToolAction(suggestTarget.CurrentTarget.GetComponent<NetworkObject>());
            //mineIntervalTimer = TickTimer.CreateFromSeconds(Runner, mineInterval);
            //Vector3 center = transform.position + Vector3.up * .5f;
            //Vector3 extents = new Vector3(gatherRanged, 1f, gatherRanged);
            //DrawLog.DrawCube(center, extents, Color.red);
            //int hitsCount = Runner.LagCompensation.OverlapBox(center, extents, Quaternion.identity, Object.InputAuthority, resourcesHit, 1 << 9, HitOptions.IncludePhysX, true);
            //if (hitsCount == 0) return;
            //HashSet<ResourceNode> test = new HashSet<ResourceNode>();
            //test = resourcesHit.Select(x => x.Collider.GetComponentInParent<ResourceNode>()).ToHashSet();
            //foreach (var i in test)
            //{
            //    Debug.Log("Collider Hit: " + i.transform.name);
            //    Debug.Log($"Node != null ? {i != null}");
            //    TryGather(i);
            //}
        }
    }
    
    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if(data.key == ConstReliableKey.GatherResult)
        {
            string json = Encoding.UTF8.GetString(data.data);
            GatherResult result = JsonConvert.DeserializeObject<GatherResult>(json);
            if (result != null)
            {
                NotificationManager.Ins.ShowNotification(result.Message, result.Success ? 1 : 2);
            }
        }
    }


    public void AddResource(int amount, ResourceType type)
    {
        Debug.Log($"AddResource called on {Runner.LocalPlayer} in {Object.InputAuthority}");
        if (!Object.HasStateAuthority) return;
        switch (type)
        {
            case ResourceType.Wood:
                resourcePlayer.Wood += amount;
                break;
            case ResourceType.Stone:
                resourcePlayer.Stone += amount;
                break;           
            case ResourceType.Gold:
                resourcePlayer.GoldOre += amount;
                break;
            case ResourceType.Electric:
                resourcePlayer.Electric += amount;
                break;
        }

    }
  
    public void ComsumpResource(NetworkResourcePlayer resource)
    {
        if (!HasStateAuthority) return;
        this.resourcePlayer.Wood -= resource.Wood;
        this.resourcePlayer.Stone -= resource.Stone;    
        this.resourcePlayer.GoldOre -= resource.GoldOre;
        this.resourcePlayer.Electric -= resource.Electric;
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
            _stats.AddModifier(mod);
        }
    }

    public void RemoveModifier(int _id)
    {
        if(HasStateAuthority)
            _stats.RemoveModifierByID(_id);
    }


    //RPC calls


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestUseTool(ToolType toolUse)
    {
        equipTool = toolUse;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestToolAction(NetworkId target)
    {
        NetworkObject obj = Runner.FindObject(target);
        if (obj == null) return;
        if ((transform.position - suggestTarget.CurrentTarget.transform.position).sqrMagnitude > gatherRanged * gatherRanged) return;

        if (suggestTarget.CurrentTarget.TryGetComponent<NetworkPlayer>(out var player))
        {
            player.TakeDamage(_stats.Get(StatsType.Damage), Object);
        }
        else if (suggestTarget.CurrentTarget.TryGetComponent<ResourceNode>(out var resourceNode))
        {
            var gatherResult = TryGather(resourceNode);           
            string json = JsonConvert.SerializeObject(gatherResult);
            byte[] sData = Encoding.UTF8.GetBytes(json);
            Runner.SendReliableDataToPlayer(Object.InputAuthority, ConstReliableKey.GatherResult, sData);            
        }
        //Lookat target here
        mineIntervalTimer = TickTimer.CreateFromSeconds(Runner, mineInterval);

    }

}
