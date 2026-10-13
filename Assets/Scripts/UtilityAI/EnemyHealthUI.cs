using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
   [SerializeField] HealthComponent healthComponent;
    [SerializeField] Image healthBar;

    private void Awake()
    {
        healthComponent ??= GetComponent<HealthComponent>();
    }

    private void Start()
    {
        healthComponent.OnHealthChanged += EnemyHealthUI_OnTakeDamage;
    }

    private void EnemyHealthUI_OnTakeDamage(float arg1, float arg2)
    {
        if(healthBar)
            healthBar.fillAmount = arg1/arg2;
    }

    private void OnDestroy()
    {
        healthComponent.OnHealthChanged -= EnemyHealthUI_OnTakeDamage;
    }
}
