using UnityEngine;
using UtilityAI.Core;

namespace UtilityAI.Considerations
{
    [CreateAssetMenu(fileName = "LevelUpUrgency", menuName = "ScriptableObjects/LevelUpUrgency")]
    public class LevelUpUrgency : Consideration
    {
        public override float ScoreConsideration(Context context)
        {
            if (context.Brain != null)
            {
                var brain = context.Brain;
                int curLevel = brain.Level;
                int expNeeded = brain.ExpToUpLevel(curLevel + 1);
                float levelUpUrgency = ((float)brain.CurrentExp / expNeeded);
                return responseCurve.Evaluate(Mathf.Clamp01(levelUpUrgency));
            }
            return 0f;
        }       
    }
}

