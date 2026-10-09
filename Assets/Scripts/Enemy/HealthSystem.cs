using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class HealthSystem: MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;
    [Header("Events")]
    public UnityEvent OnDie;
    public UnityEvent OnTakeDamage;
    public void InitializeHealth(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHealth;
        isDead = false;
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}/{maxHealth}");
        OnTakeDamage?.Invoke();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    public bool IsDead()
    {
        return isDead;
    }
    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} has died.");
        // Add death logic here (e.g., play animation, disable components, etc.)
        OnDie?.Invoke();
        gameObject.SetActive(false); // Example: deactivate the GameObject
    }
}
