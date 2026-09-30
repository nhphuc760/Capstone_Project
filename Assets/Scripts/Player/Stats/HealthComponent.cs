using System;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthComponent : NetworkBehaviour, ITakedamageable
{

    [SerializeField] float combatRegenDelay = 3f;
    [SerializeField] Image HealthBarUI;
    [Networked, OnChangedRender(nameof(OnChangeRender))]
    public int CurrentHealth { get; private set; }
    [Networked]
    TickTimer RegenDelayTimer { get; set; }

    //Runtime
    Stats _stats;
    bool _isInitialized;
    //Events
    public event Action<int, int> OnHealthChanged;
    public event Action<NetworkObject> OnDamaged; //NetworkObject: attacker
    public event Action<NetworkObject> OnDeath; //NetworkObject: killer
    //Properties
    public int MaxHealth => _stats != null ? _stats.Get(StatsType.Health) : 200;
    public int HealthRegenPerSecond => _stats != null ? _stats.Get(StatsType.HealthRegen) : 5;
    public bool IsAlive => CurrentHealth > 0;
    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0;


    public void Initialize(Stats stats)
    {
        _stats = stats;
        _isInitialized = true;
        if (HasStateAuthority)
        {
            CurrentHealth = MaxHealth;
        }
    }


    public override void Spawned()
    {
        if (HasStateAuthority && CurrentHealth <= 0 && MaxHealth > 0)
        {
            CurrentHealth = MaxHealth;
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (!_isInitialized || IsAlive) return;
        if (RegenDelayTimer.ExpiredOrNotRunning(Runner))
        {
            ApplyHealthRegen(Runner.DeltaTime);
        }
    }




    void ApplyHealthRegen(float deltaTime)
    {
        float regen = HealthRegenPerSecond * deltaTime;
        if (regen <= 0) return;

        int oldHealth = CurrentHealth;
        int newHealth = Mathf.Min(MaxHealth, CurrentHealth + Mathf.RoundToInt(regen));

        if(newHealth != oldHealth)
        {
            CurrentHealth = newHealth;
        }

    }

    public void TakeDamage(int amount, NetworkObject attacker = null)
    {
        if(IsAlive || amount  <= 0) return;
        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        RegenDelayTimer = TickTimer.CreateFromSeconds(Runner, combatRegenDelay);

        OnDamaged?.Invoke(attacker);
        if (CurrentHealth <= 0 && oldHealth > 0)
        {
            OnDeath?.Invoke(attacker);
        }

    }    


    void OnChangeRender()
    {
        if (Runner.IsResimulation) return;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        HealthBarUI.fillAmount = HealthPercent;
    }

}
