using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }
    public EnemyDataSO enemyData;
    [SerializeField] private EnemyState currentState;
    [SerializeField] private Transform currentTarget;

    private NavMeshAgent agent;
    private float lastAttackTime;
    private Animator animator;
    private HealthSystem healthSystem;
    private Transform mainBase;//manibase là tường hoặc trụ

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        healthSystem = GetComponent<HealthSystem>();
    }
    private void Start()
    {
        agent.speed = enemyData.moveSpeed;
        healthSystem.InitializeHealth(enemyData.maxHealth);
        healthSystem.OnTakeDamage.AddListener(HandleGetHit);
        healthSystem.OnDie.AddListener(HandleDie);
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
        if (currentState == EnemyState.Dead) return;

        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
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
            agent.isStopped = false;
            return;
        }

        // Xử lý Cooldown đánh
        if (Time.time >= lastAttackTime + enemyData.attackCooldown)
        {
            if (animator != null) animator.SetTrigger("Attack");
            lastAttackTime = Time.time;

            // Xoay mặt về phía mục tiêu khi đánh
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    void Attack()
    {
        // Tìm xem mục tiêu hiện tại có Interface IDamageable không
        IDamageable damageableTarget = currentTarget.GetComponent<IDamageable>();

        if (damageableTarget != null && !damageableTarget.IsDead())
        {
            damageableTarget.TakeDamage(enemyData.damage);
            Debug.Log($"{enemyData.enemyName} chém {currentTarget.name} gây {enemyData.damage} sát thương!");
        }
        else if (damageableTarget != null && damageableTarget.IsDead())
        {
            currentTarget = null;
            currentState = EnemyState.Chase;
        }
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

    public void DealDamageEvent()
    {
        if (currentTarget == null || currentState == EnemyState.Dead) return;

        float distance = GetDistanceToTargetEdge();
        if (distance <= enemyData.attackRange + 0.5f) // Cộng thêm chút sai số
        {
            IDamageable damageableTarget = currentTarget.GetComponent<IDamageable>();
            if (damageableTarget != null && !damageableTarget.IsDead())
            {
                damageableTarget.TakeDamage(enemyData.damage);
            }
        }
    }
    private void HandleGetHit()
    {
        if (currentState == EnemyState.Dead) return;
        if (animator != null) animator.SetTrigger("Hit");
    }
    private void HandleDie()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        agent.enabled = false; // Tắt agent để các con khác không bị vướng
        GetComponent<Collider>().enabled = false; // Tắt va chạm

        if (animator != null) animator.SetTrigger("Die");
        Destroy(gameObject, 3f);
    }
}
