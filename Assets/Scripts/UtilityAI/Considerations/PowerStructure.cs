using Fusion;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UtilityAI.Core;

public class PowerStructure : Consideration
{
    public override float ScoreConsideration(Context context)
    {
        if (StructureManager.Ins != null)
        {
            PlayerRef targetPlayer = context.TargetPlayer;
            if (targetPlayer == PlayerRef.None || targetPlayer == null || targetPlayer == default)
            {
                Debug.LogWarning("Target player is None");
                return 0f;
            }

            //target khong phai redzone
            var attackStructure = StructureManager.Ins.WithCategory(StructureCategory.Attack).WithPlayerRef(targetPlayer).Get();
            float totalDPS = 0f;
            foreach (var entry in attackStructure)
            {
                if (entry.Value == null) continue;
                float damageStructI = entry.Value._structureStats.Get(StatsType.Damage);
                float attackSpeedStructI = entry.Value._structureStats.Get(StatsType.AttackSpeed);
                float dpsStructI = damageStructI * attackSpeedStructI;
                totalDPS += dpsStructI;
            }
            float curHealth = context.Controller.Health.CurrentHealth;
            if (curHealth <= 0f)
            {
                Debug.LogWarning("Current health is zero or negative");
                return 0f;
            }
            responseCurve.Evaluate(Mathf.Clamp01(totalDPS / curHealth));
        }
        else
        {
            Debug.LogWarning("StructureManager not found in context");
        }
        return 0f;

    }
}
