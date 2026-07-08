using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    [Header("Spawn Setting")]
    [Tooltip("Cho phép spawn tại vị trí này.")]
    public bool canSpawn = true;

    [Tooltip("Đã có vật phẩm chiếm vị trí này hay chưa.")]
    public bool occupied = false;

    [Tooltip("Khoảng lệch ngẫu nhiên khi spawn (0 = đúng vị trí).")]
    [Min(0)]
    public float randomRadius = 0f;

    /// <summary>
    /// Trả về vị trí spawn thực tế.
    /// Nếu randomRadius > 0 thì spawn ngẫu nhiên quanh SpawnPoint.
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        if (randomRadius <= 0)
            return transform.position;

        Vector2 random = Random.insideUnitCircle * randomRadius;

        return transform.position + new Vector3(random.x, 0f, random.y);
    }

    /// <summary>
    /// Đánh dấu SpawnPoint đã có vật phẩm.
    /// </summary>
    public void SetOccupied(bool value)
    {
        occupied = value;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = occupied ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.15f);

        if (randomRadius > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, randomRadius);
        }
    }
#endif
}