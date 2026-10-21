using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] HealthComponent Health;
    [SerializeField] Image HealthBar;
    [SerializeField] TextMeshProUGUI HealthText;
    private void Start()
    {
        Health.OnHealthChanged += Health_OnHealthChanged;
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
        {
            Health.OnHealthChanged -= Health_OnHealthChanged;
        }
    }

}
