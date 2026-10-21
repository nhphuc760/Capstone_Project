using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] StaminaComponent staminaComponent;
    [SerializeField] Image staminaBar;
    [SerializeField] TextMeshProUGUI StaminaText;

    private void Awake()
    {
        staminaComponent ??= GetComponentInParent<StaminaComponent>();
    }

    private void Start()
    {
        if(staminaComponent)
        staminaComponent.OnStaminaChange += StaminaComponent_OnStaminaChange;
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
    }
}
