using System;
using Fusion;
using UnityEngine;

public class StaminaComponent : NetworkBehaviour
{
    [SerializeField] int maxStamina = 200;
    [SerializeField] int regenAmount = 5;
    [SerializeField] float regenInterval = 1f;
    [SerializeField] float regenDelayAfterUse = 2f;

    public int MaxStamina => maxStamina;
    [Networked]
    public int CurrentStamina { get; private set; }
    public bool IsExhausted => CurrentStamina <= 0;

    public event Action<int, int> OnStaminaChanged;
    public event Action OnExhausted;

    [Networked]
    TickTimer delayTimer { get; set; }
    [Networked]
    TickTimer regenIntervalTimer { get; set; }
    public override void Spawned()
    {
        if(Object.HasStateAuthority)
            CurrentStamina = maxStamina;
    }

    public override void FixedUpdateNetwork()
    {
        RegenerateStamina();
    }    

    public bool TryConsume(int amount)
    {
        if (CurrentStamina < amount) return false;
        CurrentStamina -= amount;
        CurrentStamina = Mathf.Max(0, CurrentStamina);
        delayTimer = TickTimer.CreateFromSeconds(Runner, regenDelayAfterUse);
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        if (CurrentStamina <= 0) OnExhausted?.Invoke();
        return true;
    }

    public void Restore(int amount)
    {
        CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    void RegenerateStamina()
    {
        if(CurrentStamina >= maxStamina) return;
        if (delayTimer.IsRunning) return;
        if (!regenIntervalTimer.IsRunning)
        {
            regenIntervalTimer = TickTimer.CreateFromSeconds(Runner, regenInterval);
            return;
        }

        if (regenIntervalTimer.Expired(Runner))
        {
            Restore(regenAmount);
            regenIntervalTimer = TickTimer.CreateFromSeconds(Runner, regenInterval);
        }

    }

}
