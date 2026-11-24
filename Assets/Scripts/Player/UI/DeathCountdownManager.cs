using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class DeathCountdownManager : NetworkBehaviour
{
    [SerializeField] private float respawnDuration = 10f;
    [SerializeField] DeathCountdownUI countDownUI;

    [Networked, OnChangedRender(nameof(OnDeathStateChanged))]
    public NetworkBool IsDead { get; private set; }

    [Networked, OnChangedRender(nameof(OnCountdownTimeChanged))]
    public TickTimer RespawnCountdown { get; private set; }

    public event System.Action<bool> OnDeadStateChanged; // true = dead, false = alive
    public event System.Action<float> OnCountdownUpdated; // remaining time

    private NetworkPlayer networkPlayer;

    public override void Spawned()
    {
        if (!enabled) return;
        networkPlayer = GetComponent<NetworkPlayer>();
        if (HasInputAuthority)
            countDownUI = GetComponentInChildren<DeathCountdownUI>(includeInactive: true);
        else countDownUI.gameObject.SetActive(false);
        if (networkPlayer?.Health != null)
        {
            networkPlayer.Health.OnDeath += HandlePlayerDeath;
        }
    }
    private void Update()
    {
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                RPC_TakeDamage();
            }
               
        }
    }

    //Test
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_TakeDamage()
    {
        networkPlayer.TakeDamage(20, Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (networkPlayer?.Health != null)
        {
            networkPlayer.Health.OnDeath -= HandlePlayerDeath;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!enabled) return;
        if (!HasStateAuthority) return;

        // Check if player is dead
        bool isCurrentlyDead = networkPlayer != null && !networkPlayer.Health.IsAlive;

        if (isCurrentlyDead && !IsDead)
        {
            // Player just died
            StartRespawnCountdown();
        }
        else if (!isCurrentlyDead && IsDead)
        {
            // Player respawned          
            ResetDeathState();
        }

        // Update countdown timer
        if (IsDead && RespawnCountdown.Expired(Runner))
        {
            RespawnHandle();
            // Timer is running, nothing to do - it updates automatically
        }
    }

    public override void Render()
    {
        if (!enabled) return;
        // Notify UI of countdown changes on clients with input authority
        if (HasInputAuthority && IsDead)
        {
            float remainingTime = RespawnCountdown.RemainingTime(Runner) ?? 0f;
            OnCountdownUpdated?.Invoke(remainingTime);
            countDownUI.OnCountdownUpdated(remainingTime);
        }
    }

    private void HandlePlayerDeath(NetworkObject killer)
    {
        // This is called on all clients when health reaches 0
        // State authority will handle actual state changes in FixedUpdateNetwork
        //if (HasInputAuthority)
        //{
        //    countDownUI.
        //}
    }

    private void StartRespawnCountdown()
    {
        if (!HasStateAuthority) return;

        IsDead = true;
        RespawnCountdown = TickTimer.CreateFromSeconds(Runner, respawnDuration);
        Debug.Log($"[DeathCountdownManager] Player {Object.InputAuthority} died. Respawn in {respawnDuration}s");
    }

    private void ResetDeathState()
    {
        if (!HasStateAuthority) return;

        IsDead = false;
        RespawnCountdown = TickTimer.None;      
        Debug.Log($"[DeathCountdownManager] Player {Object.InputAuthority} respawned");
    }

    void RespawnHandle()
    {
        if (StructureManager.Ins != null)
        {
            var encloseZone = StructureManager.Ins.GetEnclosedZone(Object.InputAuthority);
            Vector3? spawnPos = null;
            if(encloseZone.Count > 0)
            {
                spawnPos = encloseZone.RandomElement();
               
            }
            else
            {
                spawnPos = GameManager.Instance.SpawnPoint();
            }

            if(spawnPos != null)
                GetBehaviour<SimpleKCC>().SetPosition((Vector3)spawnPos + Vector3.one * 0.5f);
        }

        networkPlayer.Health.Respawn();
        countDownUI.OnDeathStateChanged(IsDead);
    }

    private void OnDeathStateChanged()
    {
        OnDeadStateChanged?.Invoke(IsDead);
        countDownUI.OnDeathStateChanged(IsDead);
    }

    private void OnCountdownTimeChanged()
    {
        if (HasInputAuthority)
        {
            float remainingTime = RespawnCountdown.RemainingTime(Runner) ?? 0f;
            OnCountdownUpdated?.Invoke(remainingTime);
            countDownUI.OnCountdownUpdated(remainingTime);
        }
    }

    public float GetRemainingRespawnTime()
    {
        if (!IsDead) return 0f;
        return RespawnCountdown.RemainingTime(Runner) ?? 0f;
    }
}