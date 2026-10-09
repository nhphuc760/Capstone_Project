using System;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthComponent : NetworkBehaviour
{

    [SerializeField] float combatRegenDelay = 3f;
    [SerializeField] Image HealthBarUI;
    [Networked, OnChangedRender(nameof(OnChangeRender))]
    public float CurrentHealth { get; private set; }
    [Networked]
    TickTimer DelayTimer { get; set; }
    [Networked]
    TickTimer RegenIntervalTimer { get; set; }
    //Runtime
    [Networked]
    public NetworkBool Regenable { get; set; }



    Stats _stats;
    bool _isInitialized;
    //Events
    public event Action<float, float> OnHealthChanged;// Cur-Max
    public event Action<NetworkObject> OnTakeDamage; //NetworkObject: attacker
    public event Action<NetworkObject> OnDeath; //NetworkObject: killer
    //Properties
    public float MaxHealth => _stats != null ? _stats.Get(StatsType.Health) : 200;
    public float RegenAmount => _stats != null ? _stats.Get(StatsType.HealthRegen) : 5;
    public float RegenInterval => _stats != null ? _stats.Get(StatsType.HealthRegenInterval) : 1f;
    public bool IsAlive => CurrentHealth > 0;
    public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;


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
        if (!Regenable) return;
        if (!_isInitialized || !IsAlive) return;
        RegenerateHealth();
    }



    public void Restore(float amount)
    {
        if (amount <= 0) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }



    public void TakeDamage(float amount, NetworkObject attacker = null)
    {
        if(!IsAlive || amount  <= 0) return;
        float oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        DelayTimer = TickTimer.CreateFromSeconds(Runner, combatRegenDelay);

        OnTakeDamage?.Invoke(attacker);
        if (CurrentHealth <= 0 && oldHealth > 0)
        {
            OnDeath?.Invoke(attacker);
        }

    }


    void RegenerateHealth()
    {
        if (CurrentHealth >= MaxHealth) return;
        if (!DelayTimer.ExpiredOrNotRunning(Runner)) return;
        if (!RegenIntervalTimer.IsRunning)
        {
            Debug.Log("Create RegenIntervalTimer");
            RegenIntervalTimer = TickTimer.CreateFromSeconds(Runner, RegenInterval);
            return;
        }

        if (RegenIntervalTimer.ExpiredOrNotRunning(Runner))
        {
            Debug.Log("Expired RegenIntervalTimer");
            Restore(RegenAmount);
            RegenIntervalTimer = TickTimer.CreateFromSeconds(Runner, RegenInterval);
        }

    }

    void OnChangeRender()
    {
        if (Runner.IsResimulation) return;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if(HealthBarUI != null) 
            HealthBarUI.fillAmount = HealthPercent;
    }

}
