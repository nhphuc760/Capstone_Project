using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthDoorUI : MonoBehaviour
{

    [SerializeField] HealthComponent healthComponent;
    [SerializeField] Image healthDoorUI;
    private void Awake()
    {
        healthComponent ??= GetComponent<HealthComponent>();
    }

    private void Start()
    {
        if(healthComponent)
            healthComponent.OnHealthChanged += HealthComponent_OnTakeDamage;
    }

    private void HealthComponent_OnTakeDamage(float arg1, float arg2)
    {
        if (healthDoorUI)
        {
            healthDoorUI.fillAmount = arg1/arg2;
        }
    }

    private void OnDestroy()
    {
        if(healthComponent)
         healthComponent.OnHealthChanged -= HealthComponent_OnTakeDamage;
    }
}
