using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "EnemyData/EnemyDataSO")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Basic data")]
    public string enemyName;
    public string maxHeatlh;
    public float moveSpeed;

    [Header("Attack data")]
    public float damage;
    public float attackRange;
    public float attackCooldown;

    public float aggroRange;
}
