using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    private EnemyAI enemyAI;

    void Awake()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
    }

    public void DealDamageEvent()
    {
        if (enemyAI != null)
        {
            enemyAI.DealDamageEvent();
        }
    }
}