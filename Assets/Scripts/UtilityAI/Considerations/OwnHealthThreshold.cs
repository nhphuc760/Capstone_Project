using UnityEngine;
using UtilityAI.Core;

namespace UtilityAI.Considerations
{

    [CreateAssetMenu(fileName = "HealthThreshold", menuName = "ScriptableObjects/HealthThreshold")]
    public class OwnHealthThreshold : Consideration
    {       
        public override float ScoreConsideration(Context context)
        {
            if (context.Brain == null || context.Controller?.Health == null)
                return 0f;

            var health = context.Controller.Health;
            if (health.MaxHealth <= 0f)
                return 0f;

            int level = Mathf.Max(1, context.Brain.Level);
            float t = Mathf.Clamp01((level - 1f) / Mathf.Max(1, context.Brain.MaxLevel - 1));

            // Curve: X = level chuẩn hoá, Y = ngưỡng máu (0.5 → 0.1)
            float threshold = Mathf.Clamp(responseCurve.Evaluate(t), 0.01f, 1f);

            return threshold;
            // Máu càng cao so với ngưỡng → score càng cao (RandomWeight ưu tiên ở lại)
        }
    }
}

