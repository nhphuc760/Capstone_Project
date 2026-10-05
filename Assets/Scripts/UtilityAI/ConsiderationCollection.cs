using UnityEngine;
using UtilityAI.Core;

namespace UtilityAI.Considerations
{
    [CreateAssetMenu(fileName = "NewConsiderations", menuName = "ScriptableObjects/ConsiderationCollection")]
    public class ConsiderationCollection : ScriptableObject
    {
        public Consideration[] considerations;
        public float ScoreConsiderations(Context context)
        {
            if (context == null)
            {
                Debug.Log("Context null");
                return 0;
            }
            float score = 1f;
            float modFactor = 1 - (1 / considerations.Length);
            for (int i = 0; i < considerations.Length; i++)
            {
                float considerationScore = considerations[i].ScoreConsideration(context);
                //score *= considerationScore;
                //float originalScore = score;
                float makeupValue = (1 - considerationScore) * modFactor;
                float finaleScore = considerationScore + (makeupValue * considerationScore);
                score *= finaleScore;
            }
            //Average scheme of overall score
            return score;
        }       
    }

}
