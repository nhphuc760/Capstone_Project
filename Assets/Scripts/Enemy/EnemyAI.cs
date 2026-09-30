using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }
    public EnemyDataSO enemyData;
    [SerializeField] private EnemyState currentState;
    [SerializeField] private Transform currentTarget;

    private NavMeshAgent agent;
    private float lastAttackTime;

    private Transform mainBase;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        agent.speed = enemyData.moveSpeed;
        agent.stoppingDistance = enemyData.attackRange - 0.2f;
        GameObject baseObj = GameObject.FindGameObjectWithTag("MainBase");
        if (baseObj != null)
        {
            mainBase = baseObj.transform;
        }
        else
        {
            Debug.LogError("Main Base not found in the scene. Please ensure there is a GameObject with the 'MainBase' tag.");
        }
        currentState = EnemyState.Chase;
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }
    void HandleChase()
    {
        FindTarget();

        if (currentTarget == null) return;

        agent.SetDestination(currentTarget.position);

        // Kiểm tra khoảng cách tới mục tiêu
        float distance = GetDistanceToTargetEdge();

        if (distance <= enemyData.attackRange)
        {
            currentState = EnemyState.Attack;
            agent.isStopped = true; // Dừng di chuyển khi đánh
        }
        else
        {
            agent.isStopped = false;
            CheckIfPathBlocked();
        }
    }
    void HandleAttack()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // Nếu mục tiêu chạy ra khỏi tầm đánh, quay lại Chase
        float distance = GetDistanceToTargetEdge();
        if (distance > enemyData.attackRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // Xử lý Cooldown đánh
        if (Time.time >= lastAttackTime + enemyData.attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }
    void Attack()
    {
        // TODO: Chỗ này sau sẽ gọi interface IDamageable của Target (Core, Player hoặc Tường) để trừ máu
        Debug.Log($"{enemyData.enemyName} chém {currentTarget.name} gây {enemyData.damage} sát thương!");
    }
    void FindTarget()
    {
        Transform closestTarget = GetClosestPlayerInAggro();
        if(closestTarget != null)
        {
            currentTarget = closestTarget;
            return;
        }
        if (mainBase != null)
        {
            currentTarget = mainBase;
        }
    }

    Transform GetClosestPlayerInAggro()
    {
        // Sau này sẽ dùng Physics.OverlapSphere hoặc hệ thống EntityList quản lý để tối ưu hiệu năng thay vì FindGameObjectsWithTag.
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float minDistance = enemyData.aggroRange;

        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist <= minDistance)
            {
                minDistance = dist;
                closest = p.transform;
            }
        }
        return closest;
    }
    void CheckIfPathBlocked()
    {
        // Xử lý Anti-Maze: Nếu người chơi xây tường bít kín đường tới Base
        if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            Debug.LogWarning("Đường bị chặn! Chuẩn bị logic đập bức tường cản đường gần nhất.");
            // TODO: Bắn OverlapSphere tìm Tường và gán Tường làm currentTarget.
        }
    }
    float GetDistanceToTargetEdge()
    {
        Collider targetCollider = currentTarget.GetComponent<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closestPoint);
        }
        return Vector3.Distance(transform.position, currentTarget.position);
    }
}
