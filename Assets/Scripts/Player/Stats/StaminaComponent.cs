using System;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class StaminaComponent : NetworkBehaviour
{
    [SerializeField] float regenInterval = 1f; // mất bao nhiêu giây hồi 1 lần
    [SerializeField] float regenDelayAfterUse = 2f; //Delay sau khi consume
    [SerializeField] Image StaminaBarUI;

    //Networked
    [Networked, OnChangedRender(nameof(OnChangeRender))] public int CurrentStamina { get; private set; }
    [Networked] TickTimer DelayTimer { get; set; }
    [Networked] TickTimer RegenIntervalTimer { get; set; }

    //Runtime
    Stats _stats;
    bool _isInitialized;

    //Events
    public event Action<int, int> OnStaminaChange;//current, max
    public event Action OnExhausted;// Cạn kiệt


    public int MaxStamina => _stats != null ? _stats.Get(StatsType.Stamina) : 200;

    public int RegentAmount => _stats != null ? _stats.Get(StatsType.StaminaRegen) : 5;

    public bool IsExhausted => CurrentStamina <= 0;
    public float StaminaPercent => MaxStamina > 0 ? (float)CurrentStamina / MaxStamina : 0;


    public void Initialize(Stats stats)
    {
        _stats = stats;
        _isInitialized = true;
        if(HasStateAuthority)
        {
            CurrentStamina = MaxStamina;
        }
    }

    public override void Spawned()
    {
        if(HasStateAuthority && CurrentStamina <= 0 && MaxStamina > 0)
        {
            CurrentStamina = MaxStamina;
        }
    }

    public override void FixedUpdateNetwork() // Gọi trên cả host và client với mong muốn predict trên client
    {
        if (!_isInitialized) return;
        RegenerateStamina();
    }    

    public bool TryConsume(int amount) // vì input nhận được trên cả host và client nên hàm này được gọi trên cả 2
    {
        if (amount <= 0) return false;
        if (CurrentStamina < amount) return false;
        CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
        DelayTimer = TickTimer.CreateFromSeconds(Runner, regenDelayAfterUse);
        if(CurrentStamina <= 0)
        {
            OnExhausted?.Invoke(); 
        }
        return true;
    }

    public void Restore(int amount)
    {
        if (amount <= 0) return;
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);       
    }


    void OnChangeRender()
    {
        if (Runner.IsResimulation) return;
        OnStaminaChange?.Invoke(CurrentStamina, MaxStamina);
        StaminaBarUI.fillAmount = (float)CurrentStamina / MaxStamina;
    }
    void RegenerateStamina()
    {
        Debug.Log("Remaining time: " + DelayTimer.RemainingTime(Runner));
        if(CurrentStamina >= MaxStamina) return;
        if (!DelayTimer.ExpiredOrNotRunning(Runner)) return;
        if (!RegenIntervalTimer.IsRunning)
        {
            Debug.Log("Create RegenIntervalTimer");
            RegenIntervalTimer = TickTimer.CreateFromSeconds(Runner, regenInterval);
            return;
        }

        if (RegenIntervalTimer.ExpiredOrNotRunning(Runner))
        {
            Debug.Log("Expired RegenIntervalTimer");
            Restore(RegentAmount);
            RegenIntervalTimer = TickTimer.CreateFromSeconds(Runner, regenInterval);
        }

    }

}
