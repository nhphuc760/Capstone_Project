using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilityAI.Core;

public class EnemyHealthUI : MonoBehaviour
{
   [SerializeField] HealthComponent healthComponent;
    [SerializeField] AIBrain aiBrain;
    [SerializeField] TextMeshProUGUI Lv;
    
    [SerializeField] Image healthBar;
    private void Awake()
    {
        healthComponent ??= GetComponentInParent<HealthComponent>();
        aiBrain ??= GetComponentInParent<AIBrain>();
    }

    private void Start()
    {
        healthComponent.OnHealthChanged += EnemyHealthUI_OnTakeDamage;
        aiBrain.OnLevelUp += AiBrain_OnLevelUp;
        Util.Core.Utils.DelayCall(1f, () => { AiBrain_OnLevelUp(); }).Forget();
    }

    private void AiBrain_OnLevelUp()
    {
        if(Lv)
            Lv.text = aiBrain.Level.ToString();
    }

    private void EnemyHealthUI_OnTakeDamage(float arg1, float arg2)
    {
        if(healthBar)
            healthBar.fillAmount = arg1/arg2;
    }

    private void OnDestroy()
    {

        healthComponent.OnHealthChanged -= EnemyHealthUI_OnTakeDamage;
        if(aiBrain)
            aiBrain.OnLevelUp -= AiBrain_OnLevelUp;
    }
}
