using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] StaminaComponent staminaComponent;
    [SerializeField] Stats _statsPlayer;
    [SerializeField] Image staminaBar;
    [SerializeField] TextMeshProUGUI StaminaText;
    [SerializeField] TextMeshProUGUI StaminaRegen;
    private void Awake()
    {
        staminaComponent ??= GetComponentInParent<StaminaComponent>();
    }

    private void Start()
    {
        if(staminaComponent)
        staminaComponent.OnStaminaChange += StaminaComponent_OnStaminaChange;
        if (_statsPlayer)
            _statsPlayer.DirtyChanged += DirtyChanged;
        Util.Core.Utils.DelayCall(1f, DirtyChanged).Forget();
    }

    private void DirtyChanged()
    {
        if(StaminaRegen)
            StaminaRegen.text = $"+ {(1 / staminaComponent.RegenInterval) * staminaComponent.RegenAmount}/s";
    }

    private void StaminaComponent_OnStaminaChange(float arg1, float arg2)
    {
        if (staminaBar)
            staminaBar.fillAmount = arg1 / arg2;
        if (StaminaText)
            StaminaText.text = $"{(int)arg1}/{(int)arg2}";
    }



    private void OnDestroy()
    {
        if (staminaComponent)
            staminaComponent.OnStaminaChange -= StaminaComponent_OnStaminaChange;
        if (_statsPlayer)
            _statsPlayer.DirtyChanged -= DirtyChanged;
    }
}
