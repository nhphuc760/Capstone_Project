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


public class NetworkPlayer : NetworkBehaviour, IAffector, ITakedamageable, IInteractable
{
    [Flags]
    public enum PlayerState : byte
    {
        None,
        Interacting,
        Shopping,

    }
    public static NetworkPlayer Local { get; private set; }
    public HealthComponent Health { get; private set; }
    public StaminaComponent Stamina { get; private set; }

    [SerializeField] GameObject CanvasWorldSpace;
    [SerializeField] GameObject CanvasOverlay;
    [SerializeField] TooltipUI TooltipUI;
    [SerializeField] StructureUI StructureUI;
    [SerializeField] InteractionSystem interactionSystem;
    [SerializeField] InteractionData InteractionData;
    [SerializeField] GameObject Axe;
    [SerializeField] GameObject PickAxe;    
    [SerializeField] GameObject Screwdriver;
    [SerializeField] Animator animator;
    public Stats _stats { get; private set; }
    

    [SerializeField] SimpleKCC controller;
    [SerializeField] Camera cameraView;
    [SerializeField] float jumpForce = 8f;

    [Networked] NetworkInputData previousInput { get; set; }
    Vector3 lastInputDir;
    public NetworkInventory inventory { get; private set; }

    //Resource Exploitation
    [Networked, OnChangedRender(nameof(OnEquipTool))] public ToolType equipTool { get; set; }
    [SerializeField] SuggestTarget suggestTarget;
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

    public Transform InteractTransform => transform;

    public Action OnCompleted { get; set ; }


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
        controller.SetGravity(Physics.gravity.y * 2f);
        gameObject.name = Object.InputAuthority.ToString();
        inventory ??= GetComponent<NetworkInventory>();
        Health ??= GetComponent<HealthComponent>();
        Stamina ??= GetComponent<StaminaComponent>();
        _stats ??= GetComponent<Stats>();
        //Suggest Target
        suggestTarget ??= GetComponent<SuggestTarget>();
        suggestTarget.enabled = true;
        Health.Initialize(_stats);
        Stamina.Initialize(_stats);
        if (HasInputAuthority)
        {

            //Subcribe Inventory Event

            cameraView.tag = "MainCamera";

            // Disable Stats WorldSpace
            CanvasWorldSpace.GetComponentInChildren<PlayerHealthUI>().gameObject.SetActive(false);
            CanvasWorldSpace.GetComponentInChildren<PlayerStaminaUI>().gameObject.SetActive(false);
            //CanvasWorldSpace.gameObject.SetActive(false);


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
            relialeEvent = new EventBinding<ReliableReceiveData>(OnReliableReceiveData);
            EventBus<ReliableReceiveData>.Register(relialeEvent);
        }

        if (HasStateAuthority)
        {
            doorPlayer = Runner.Spawn(doorPrefab, inputAuthority: Object.InputAuthority).GetBehaviour<Door>();
            doorPlayer.RPC_SetActiveNetworked(false);
        }


        //Cheats

        resourcePlayer.Wood = 100;

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
                    TooltipUI.Show(context.GetContent(), Input.mousePosition, hit.point);
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
            Vector3 inputDirection = data.moveDirection.normalized;
            Vector3 moveDirection = inputDirection.normalized * _stats.Get(StatsType.MoveSpeed);
            float jumpImpluse = 0f;
            if (data.button.WasPressed(previousInput.button, ButtonType.Jump) && controller.IsGrounded)
            {
                jumpImpluse = jumpForce;
            }
            if (data.button.WasPressed(previousInput.button, ButtonType.EquipTool))
            {
                if (Object.HasStateAuthority)
                {
                    equipTool = ToolType.Axe;
                }               
            }

            if (data.button.WasPressed(previousInput.button, ButtonType.Interact))
            {
                interactionSystem.Pressed();
            }

            if (data.button.IsSet(ButtonType.Interact))
            {
                interactionSystem.Hold(Runner.DeltaTime);
            }

            if (data.button.WasReleased(previousInput.button, ButtonType.Interact))
            {
                interactionSystem.Released();
            }
            
            if (data.button.WasPressed(previousInput.button, ButtonType.TestAddModify))
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
            previousInput = data;           
            lastInputDir = inputDirection;
            animator.SetBool("IsWalking", inputDirection.sqrMagnitude > 0);
            controller.Move(moveDirection, jumpImpluse);   
            
        }

        if (transform.position.y <= -10f)
        {
            controller.SetPosition(Vector3.one);
        }
    }



    public override void Render()
    {
        Transform model = transform.GetChild(0);
        float angularSpeed = 15f;
        if (lastInputDir.sqrMagnitude > 0)
        {
            model.localRotation = Quaternion.Lerp(model.localRotation, Quaternion.LookRotation(lastInputDir), angularSpeed * Runner.DeltaTime);
        }
        model.localPosition = Vector3.zero;
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
    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if (data.key == ConstReliableKey.GatherResult)
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
        if (HasStateAuthority)
            _stats.RemoveModifierByID(_id);
    }


    void OnEquipTool()
    {
        switch (equipTool)
        {
            case ToolType.None:
                Axe.SetActive(false);
                PickAxe.SetActive(false);
                Screwdriver.SetActive(false);
                break;
            case ToolType.Axe:
                Axe.SetActive(true);
                PickAxe.SetActive(false);
                Screwdriver.SetActive(false);
                break;
            case ToolType.PickAxe:
                Axe.SetActive(false);
                PickAxe.SetActive(true);
                Screwdriver.SetActive(false);
                break;
            case ToolType.Screwdriver:
                Axe.SetActive(false);
                PickAxe.SetActive(false);
                Screwdriver.SetActive(true);
                break;
            default:
                break;
        }

    }

    //RPC calls


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestUseTool(ToolType toolUse)
    {
        equipTool = toolUse;
    }  
    //IInteractable implementation

    public InteractionData Data(NetworkPlayer interactor) => InteractionData;

    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
        if (!interactor.Health.IsAlive)
        {
            return new InteractionResult(false, "Ngỏm củ tỏi rồi!");
        }
        if (interactor.Stamina.CurrentStamina < 10)
        {
            return new InteractionResult(false, "Không đủ thể lực!");
        }
        if (interactor.equipTool != ToolType.Axe && interactor.equipTool != ToolType.PickAxe)
        {
            return new InteractionResult(false);
        }
        return new InteractionResult(true);
    }

    public void OnInteract(InteractionContext ctx)
    {
        TakeDamage(ctx.Interactor._stats.Get(StatsType.Damage), ctx.Interactor.Object);
    }
}
