using Fusion;
using UnityEngine;

public class SurvivalStats : NetworkBehaviour
{
    [Header("Survival Limits")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxStamina = 100;

    [Networked] public int health { get; private set; }
    [Networked] public int stamina { get; private set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            health = maxHealth;
            stamina = maxStamina;
        }
    }

    public void takeDamage(int amount)
    {
        if (!HasStateAuthority) return;

        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth); 
    }

    public void heal(int amount)
    {
        if (!HasStateAuthority) return;

        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public void consumeStamina(int amount)
    {
        if (!HasStateAuthority) return;

        stamina -= amount;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }
    public void regenStamina(int amount)
    {
        if (!HasStateAuthority) return;

        stamina += amount;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public bool isDead()
    {
        return health <= 0;
    }

    public bool isStaminaEmpty()
    {
        return stamina <= 0;
    }
}
