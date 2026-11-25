using System;
using Cysharp.Threading.Tasks;
using FischlWorks_FogWar;
using Fusion;
using Fusion.Addons.SimpleKCC;
using SilentCastle.MonsterAI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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


public class NetworkPlayer : NetworkBehaviour, IAffector, ITakedamageable, IMonsterTarget
{
    private PlayerInputProvider _inputProvider;
    private bool _isGameplayInitialized;

    [Flags]
    public enum PlayerState : byte
    {
        None,
        Interacting,
        Shopping,
    }
    public static NetworkPlayer Local { get; private set; }

    [Networked] public NetworkString<_32> Name { get; private set; } = default;
    [Networked] public bool Lock { get; set; }

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

    [Networked] int level { get; set; }

    public int Level => level;

    [SerializeField] SimpleKCC controller;
    [SerializeField] Camera cameraView;

    [Networked] NetworkInputData previousInput { get; set; }
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

    public NetworkId TargetId => Object.Id;

    public TargetKind Kind => TargetKind.Player;

    public Vector3 Position => transform.position;

    public bool IsAlive => Health != null && Health.IsAlive;

    public float Health01 => Health != null ? Health.HealthPercent : 1f;

    public float BodyRadius => 0.6f;

    public bool IsTargetable => IsAlive;


    [SerializeField] NetworkQuestSys questSystem;


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Object != null && Object.IsValid && scene.name != "LobbyScene")
        {
            InitializeGameplayDelayedAsync().Forget();
        }
    }

    private async UniTaskVoid InitializeGameplayDelayedAsync()
    {
        // Đợi 2 frame để Photon Fusion hoàn tất nạp scene mới trước khi spawn Door và đăng ký hệ thống
        await UniTask.DelayFrame(2);
        if (this == null || Object == null || !Object.IsValid) return;
        InitializeGameplayIfNeeded();
    }

    public override void Spawned()
    {
        if (Runner.LocalPlayer == Object.InputAuthority && Local == null)
        {
            Local = this;
        }
        questSystem ??= GetComponent<NetworkQuestSys>();
        // Chặn toàn bộ HUD, Camera gameplay và trọng lực khi đang ở LobbyScene
        if (SceneManager.GetActiveScene().name == "LobbyScene")
        {
            if (cameraView != null) cameraView.gameObject.SetActive(false);
            if (CanvasOverlay != null) CanvasOverlay.SetActive(false);
            if (CanvasWorldSpace != null) CanvasWorldSpace.SetActive(false);
            if (controller != null) controller.enabled = false;
            gameObject.name = Object.InputAuthority.ToString();
            return;
        }

        InitializeGameplayIfNeeded();
    }

    private void InitializeGameplayIfNeeded()
    {
        if (_isGameplayInitialized) return;
        _isGameplayInitialized = true;

        if (CanvasWorldSpace != null) CanvasWorldSpace.SetActive(true);

        if (!Object.HasInputAuthority)
        {
            if (cameraView != null) cameraView.gameObject.SetActive(false);
            if (CanvasOverlay != null) CanvasOverlay.SetActive(false);
        }
        else
        {
            if (cameraView != null) cameraView.gameObject.SetActive(true);
            if (CanvasOverlay != null) CanvasOverlay.SetActive(true);
        }

        if (controller != null)
        {
            controller.enabled = true;
            controller.SetGravity(Physics.gravity.y * 2f);
        }

        gameObject.name = Object.InputAuthority.ToString();
        inventory ??= GetComponent<NetworkInventory>();
        Health ??= GetComponent<HealthComponent>();
        Stamina ??= GetComponent<StaminaComponent>();
        _stats ??= GetComponent<Stats>();

        suggestTarget ??= GetComponent<SuggestTarget>();
        if (suggestTarget != null) suggestTarget.enabled = true;

        Health.Initialize(_stats);
        Stamina.Initialize(_stats);
        level = 1;

        if (WorldContext.Instance != null)
            WorldContext.Instance.Register(this);

        if (Object.HasInputAuthority)
        {
            if (Runner.GetComponent<BasicSpawner>() == null)
            {
                _inputProvider = gameObject.GetComponent<PlayerInputProvider>() ?? gameObject.AddComponent<PlayerInputProvider>();
                Runner.AddCallbacks(_inputProvider);
                Runner.ProvideInput = true;
            }

            if (CanvasWorldSpace != null)
            {
                var healthUI = CanvasWorldSpace.GetComponentInChildren<PlayerHealthUI>(true);
                if (healthUI != null) healthUI.gameObject.SetActive(false);
                var staminaUI = CanvasWorldSpace.GetComponentInChildren<PlayerStaminaUI>(true);
                if (staminaUI != null) staminaUI.gameObject.SetActive(false);
            }

            var inventoryUI = GetComponentInChildren<InventoryUI>(includeInactive: true);
            if (inventoryUI != null)
            {
                inventoryUI.SetInventory(inventory);
                inventory.OnInventoryChanged += inventoryUI.OnInventoryChanged;
            }

            foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam != cameraView && cam.CompareTag("MainCamera"))
                {
                    cam.gameObject.SetActive(false);
                }
            }

            if (cameraView != null)
            {
                cameraView.gameObject.SetActive(true);
                cameraView.tag = "MainCamera";
            }

            // Chỉ đăng ký Fog of War cho nhân vật của máy mình
            fogSpawner = FindFirstObjectByType<csRevealerSpawner>();
            if (fogSpawner != null)
            {
                fogSpawner.RegisterPlayer(transform);
            }
            else
            {
                Debug.LogError("[FogWar] Cannot find csRevealerSpawner!");
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterCamera(Object.InputAuthority, cameraView);

        if (HasStateAuthority && doorPlayer == null && doorPrefab != default)
        {
            doorPlayer = Runner.Spawn(doorPrefab, inputAuthority: Object.InputAuthority).GetBehaviour<Door>();
            doorPlayer.RPC_SetActiveNetworked(false);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isGameplayInitialized = false;

        if (_inputProvider != null && runner != null)
        {
            runner.RemoveCallbacks(_inputProvider);
            _inputProvider = null;
        }
        if (fogSpawner != null)
        {
            fogSpawner.UnregisterPlayer();
        }
        if (WorldContext.Instance != null) WorldContext.Instance.Unregister(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterCamera(Object.InputAuthority);
        }
    }

    private void OnApplicationQuit()
    {
        if (Object != null && Object.HasInputAuthority && Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown();
        }
    }

    private void Update()
    {
        if (GameplayPauseManager.IsPaused) return;
        // Không chạy raycast Tooltip / BuildSystem / phím cheat khi đang ở LobbyScene hoặc chưa init xong
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;
        if (!_isGameplayInitialized) return;

        // Killed Monster
        if (Input.GetKeyDown(KeyCode.K) && HasInputAuthority)
        {
            var monster = FindAnyObjectByType<MonsterBrain>();
            if (monster != null)
                monster.GetComponent<ITakedamageable>().TakeDamage(50, Object);
        }

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(1) && !pointerOverUI && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
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

        if (Input.GetMouseButtonDown(0) && !pointerOverUI && Camera.main != null)
        {
            if (!string.IsNullOrEmpty(GetComponent<BuildSystem>().idStruct.Value)) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.root.TryGetComponent<StructureBase>(out var structure) && (structure.Object.InputAuthority == Runner.LocalPlayer))
                {
                    Debug.Log(hit.transform.root.name);
                    StructureUI.Show(structure);
                }
                else
                {
                    StructureUI.Hide();
                }
            }
        }

        // Cheat
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            RPC_CheatResource();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;
        if (!_isGameplayInitialized || _stats == null) return;
        if (Lock) return;

        _stats.Tick(Runner.DeltaTime);
        if (GetInput(out NetworkInputData data))
        {
            if (!Health.IsAlive) return;
            Vector3 inputDirection = data.moveDirection.normalized;
            Vector3 moveDirection = inputDirection.normalized * _stats.Get(StatsType.MoveSpeed);
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
            controller.Move(moveDirection);
        }
    }

    public override void Render()
    {
        Transform model = transform.GetChild(0);
        float angularSpeed = 720f;
        if (previousInput.moveDirection.sqrMagnitude > 0)
        {
            animator.SetBool("IsWalking", true);
            Quaternion targetRotation = Quaternion.LookRotation(previousInput.moveDirection);
            model.localRotation = Quaternion.RotateTowards(model.localRotation, targetRotation, angularSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("IsWalking", false);
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
        questSystem?.AddResourceProgress(type, amount);
        OnResourceGathered?.Invoke(type, amount);

    }

    public void AddResource(NetworkResourcePlayer resources)
    {
        if (!HasStateAuthority) return;

        // Add resources
        resourcePlayer.Wood += resources.Wood;
        resourcePlayer.Stone += resources.Stone;
        resourcePlayer.GoldOre += resources.GoldOre;
        resourcePlayer.Electric += resources.Electric;

        // Update Quest Progress
        if (resources.Wood > 0)
            questSystem?.AddResourceProgress(ResourceType.Wood, resources.Wood);

        if (resources.Stone > 0)
            questSystem?.AddResourceProgress(ResourceType.Stone, resources.Stone);

        if (resources.GoldOre > 0)
            questSystem?.AddResourceProgress(ResourceType.Gold, resources.GoldOre);

        if (resources.Electric > 0)
            questSystem?.AddResourceProgress(ResourceType.Electric, resources.Electric);
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
            Debug.Log(JsonUtility.ToJson(mod));
        }
    }

    public void RemoveModifier(int _id)
    {
        if (HasStateAuthority)
        {
            Debug.Log("Remove by" + _id);
            _stats.RemoveModifierByID(_id);
        }
    }

    // RPC calls
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_CheatResource()
    {
        AddResource(new NetworkResourcePlayer
        {
            Wood = 100,
            Stone = 100,
            GoldOre = 100,
            Electric = 100,
        });
    }

    public void ReceiveDamage(float amount, MonsterBrain attacker)
    {
        Health.TakeDamage(amount, attacker.Object);
    }

    // IInteractable implementation
    #region Interactions

    #endregion
}