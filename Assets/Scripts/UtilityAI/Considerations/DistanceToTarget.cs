using UnityEngine;
using UnityEngine.AI;
using UtilityAI.Core;


namespace UtilityAI.Considerations 
{
    [CreateAssetMenu(fileName = "DistanceToTarget", menuName = "ScriptableObjects/DistanceConsider")]
    public class DistanceToTarget : Consideration
    {
        public override float ScoreConsideration(Context context)
        {
            if (context.TryGetValue<Vector3>(ContextKey.CandidatePlaceTarget, out Vector3 candidateTarget))
            {
                if (context.Agent != null)
                {
                    var path = new NavMeshPath();

                    if (context.Agent.CalculatePath(candidateTarget, path))
                    {
                        float wMap = context.Get<float>(ContextKey.WidthMap);
                        //float hMap = context.Get<float>(ContextKey.HeightMap);
                        float maxDistance = Mathf.Sqrt(2) * wMap;
                        float actualDistance = PathDistance(path);
                        //diem tang dan
                        return responseCurve.Evaluate(Mathf.Clamp01(actualDistance/maxDistance));
                    }
                }
                else
                {
                    Debug.LogWarning("Agent null");
                }
            }
            else
            {
                Debug.LogWarning("Target null");
            }
                return 0;
        }

        float PathDistance(NavMeshPath path)
        {
            if(path == null)
            {
                Debug.LogWarning("PathCorners Null");
                return 0; 
            }
            float distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distance;
        }

        
    }
}



