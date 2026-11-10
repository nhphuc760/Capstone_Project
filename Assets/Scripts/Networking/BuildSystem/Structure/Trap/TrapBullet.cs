using Fusion;
using UnityEngine;

public class TrapBullet : MonoBehaviour
{
    [SerializeField] Trap trapParent;
    [SerializeField] Animator animator;
    Transform Target;
    float timer = 0f;
    float deltaDistance;
    [Tooltip("Thời gian di chuyển tới mục tiêu")]
    float speedTime = 1f;
    Vector3 startPosition;
    void FixedUpdate()
    {
        if (Target != null) 
        {
            timer += Time.fixedDeltaTime;
            deltaDistance = timer / speedTime;
            transform.position = Vector3.Lerp(startPosition, TargetPoint(), deltaDistance);
            if ((TargetPoint() - transform.position).sqrMagnitude <= 0.01f)
            {
                CollisionTarget();
            }
        }

    }


    public void SetTarget(Transform target, float speedTime = 1f)
    {
        Target = target;
        this.speedTime = speedTime;
        timer = 0f;
        startPosition = transform.position;
        animator.Play("Rotate");
    }


    void CollisionTarget()
    {
        if (!Target.CompareTag("Enemy")) return;
        trapParent.RequestStunTarget();
        Destroy(gameObject);
    }


    Vector3 TargetPoint()
    {
        return Target.position + Vector3.up * 0.5f;
    }
    
}
