using SilentCastle.MonsterAI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
   [SerializeField] HealthComponent healthComponent;
    [SerializeField] MonsterBrain aiBrain;
    [SerializeField] TextMeshProUGUI Lv;
    
    [SerializeField] Image healthBar;
    private void Awake()
    {
        healthComponent ??= GetComponentInParent<HealthComponent>();
        aiBrain ??= GetComponentInParent<MonsterBrain>();
    }

    private void Start()
    {
        healthComponent.OnHealthChanged += EnemyHealthUI_OnTakeDamage;
        aiBrain.LevelUpPlayed += AiBrain_OnLevelUp;
        //Util.Core.Utils.DelayCall(1f, () => { AiBrain_OnLevelUp(); }).Forget();
    }

    private void AiBrain_OnLevelUp(int lvl)
    {
        if(Lv)
            Lv.text = lvl.ToString();
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
            aiBrain.LevelUpPlayed -= AiBrain_OnLevelUp;
    }
}
