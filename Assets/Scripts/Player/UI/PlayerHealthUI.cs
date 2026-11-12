using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] HealthComponent Health;
    [SerializeField] Stats _statsPlayer;
    [SerializeField] Image HealthBar;
    [SerializeField] TextMeshProUGUI HealthText;
    [SerializeField] TextMeshProUGUI HealthRegen;
    private void Start()
    {
        if(Health)
            Health.OnHealthChanged += Health_OnHealthChanged;
        if (_statsPlayer)
            _statsPlayer.DirtyChanged += DirtyChanged;
        Util.Core.Utils.DelayCall(1f, DirtyChanged).Forget();
    }

    private void DirtyChanged()
    {
        if (HealthRegen)
            HealthRegen.text = $"+ {(1/Health.RegenInterval) * Health.RegenAmount}/s";
    }

    private void Health_OnHealthChanged(float arg1, float arg2)
    {        
        if (HealthBar)
            HealthBar.fillAmount = arg1 / arg2;
        if (HealthText)
            HealthText.text = $"{(int)arg1 }/{ (int)arg2}";
    }

    private void OnDestroy()
    {
        if (Health != null)        
            Health.OnHealthChanged -= Health_OnHealthChanged;        
        if (_statsPlayer)
            _statsPlayer.DirtyChanged -= DirtyChanged;
    }

}
