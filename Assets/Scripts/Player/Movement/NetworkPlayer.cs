using System;
using FischlWorks_FogWar;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public struct NetworkResourcePlayer : INetworkStruct
{
    public int Wood;
    public int Stone;
    public int GoldOre;
    public int Electric;
    public static bool operator >=(NetworkResourcePlayer left, NetworkResourcePlayer right)
    {
        return left.Wood >= right.Wood
            && left.Stone >= right.Stone
            && left.GoldOre >= right.GoldOre
            && left.Electric >= right.Electric;
    }
    public static bool operator <=(NetworkResourcePlayer left, NetworkResourcePlayer right)
    {
        return left.Wood <= right.Wood
            && left.Stone <= right.Stone
            && left.GoldOre <= right.GoldOre
            && left.Electric <= right.Electric;
    }
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
    [Flags]
    public enum PlayerState : byte
    {
        None,
        Interacting,
        Shopping,

    }
    public static NetworkPlayer Local { get; private set; }

    [Networked] public NetworkString<_32> Name { get; private set; } = default;

    public HealthComponent Health { get; private set; }
    public StaminaComponent Stamina { get; private set; }
    private csRevealerSpawner fogSpawner;//Fog of war
    [SerializeField] GameObject CanvasWorldSpace;
    [SerializeField] GameObject CanvasOverlay;
    [SerializeField] TooltipUI TooltipUI;
    [SerializeField] StructureUI StructureUI;
    [Header("Interactions")]
    [SerializeField] Animator animator;
    public Stats _stats { get; private set; }


    [SerializeField] SimpleKCC controller;
    [SerializeField] Camera cameraView;

    [Networked] NetworkInputData previousInput { get; set; }
    Vector3 lastInputDir;
    public NetworkInventory inventory { get; private set; }

    //Resource Exploitation
    [SerializeField] SuggestTarget suggestTarget;
    [Networked, OnChangedRender(nameof(OnResourceChanged))]
    public ref NetworkResourcePlayer resourcePlayer => ref MakeRef<NetworkResourcePlayer>();
    public event Action<NetworkResourcePlayer> OnResouceChange;
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
            if (inventoryUI != null)
            {
                inventoryUI.SetInventory(inventory);
                inventory.OnInventoryChanged += inventoryUI.OnInventoryChanged;
            }

        }

        if (HasStateAuthority)
        {
            doorPlayer = Runner.Spawn(doorPrefab, inputAuthority: Object.InputAuthority).GetBehaviour<Door>();
            doorPlayer.RPC_SetActiveNetworked(false);
        }
        // Fog of War
        if (Object.HasInputAuthority)
        {
            fogSpawner = FindFirstObjectByType<csRevealerSpawner>();

            if (fogSpawner != null)
            {
                Debug.Log(
                    $"[FogWar] Register Player: {Object.InputAuthority} | " +
                    $"LocalPlayer: {Runner.LocalPlayer}"
                );

                fogSpawner.RegisterPlayer(transform);
            }
            else
            {
                Debug.LogError(
                    $"[FogWar] Cannot find csRevealerSpawner on {Runner.LocalPlayer}!"
                );
            }
        }

        //Cheats

        resourcePlayer.Wood = 100;

    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority && fogSpawner != null)
        {
            Debug.Log($"[FogWar] Unregister Player: {Object.InputAuthority}");

            fogSpawner.UnregisterPlayer();
            fogSpawner = null;
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
            if (!string.IsNullOrEmpty(GetComponent<BuildSystem>().idStruct.Value)) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.root.TryGetComponent<StructureBase>(out var context) && context.Object.InputAuthority == Object.InputAuthority)
                {
                    Debug.Log(hit.transform.root.name);
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
                jumpImpluse = _stats.Get(StatsType.Force);
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
    public void AddResource(NetworkResourcePlayer resouces)
    {
        if (!HasStateAuthority) return;
        resourcePlayer.Wood += resouces.Wood;
        resourcePlayer.Stone += resouces.Stone;
        resourcePlayer.GoldOre += resouces.GoldOre;
        resourcePlayer.Electric += resouces.Electric;
    }

    public void ComsumpResource(NetworkResourcePlayer resource)
    {
        if (!HasStateAuthority) return;
        this.resourcePlayer.Wood -= resource.Wood;
        this.resourcePlayer.Stone -= resource.Stone;
        this.resourcePlayer.GoldOre -= resource.GoldOre;
        this.resourcePlayer.Electric -= resource.Electric;
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

    //RPC calls
    //IInteractable implementation
    #region Interactions
   

    #endregion

}


