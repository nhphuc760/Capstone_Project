using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthDoorUI : MonoBehaviour
{

    [SerializeField] HealthComponent healthComponent;
    [SerializeField] Image healthDoorUI;
    [SerializeField] float showDuration = 3f;
    float timer = 0;
    private void Awake()
    {
        healthComponent ??= GetComponent<HealthComponent>();
    }

    private void Start()
    {
        if(healthComponent)
            healthComponent.OnHealthChanged += HealthComponent_OnTakeDamage;
        gameObject.SetActive(false);
    }



    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }



    private void HealthComponent_OnTakeDamage(float arg1, float arg2)
    {
        gameObject.SetActive(true);
        timer = showDuration;
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
