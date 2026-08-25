using System;
using Fusion;
using UnityEngine;

public class HealthComponent : NetworkBehaviour
{
    [SerializeField] int maxHealth;
    public int MaxHealth => maxHealth;

    [Networked]
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDeath;


    public override void Spawned()
    {
        CurrentHealth = MaxHealth;
    }

    public void Health(int amount)
    {
        if (!IsAlive) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void ReviveAt(float percentage = 1f)
    {
        CurrentHealth = Mathf.RoundToInt(maxHealth * Mathf.Clamp01(percentage));
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth); 
    }

}
