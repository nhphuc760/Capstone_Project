using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

public class TonadoMovement : MonoBehaviour
{
    [Header("Movement")]
    // Bán kính cơn lốc được phép chọn điểm đến ngẫu nhiên 
    [SerializeField] private float moveRadius = 80f;
    // Khoảng cách đến đích.
    [SerializeField] private float arriveDistance = 2f;

    [Header("Raycast")]
    // Độ dài của 3 tia Raycast dùng để phát hiện vật cản phía trước.
    [SerializeField] private float rayDistance = 8f;
    // Góc lệch của tia trái và tia phải so với tia chính.
    [SerializeField] private float rayAngle = 35f;
    // Layer chứa các vật cản
    [SerializeField] private LayerMask obstacleLayer;

    private NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.autoBraking = false;

        PickRandomDestination();
    }
    void Update()
    {
        CheckObstacle();
        // Nếu đã tới đích thì chọn điểm mới
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            PickRandomDestination();
        }

        // Quay theo hướng đang di chuyển
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion target =
                Quaternion.LookRotation(agent.velocity.normalized);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                5f * Time.deltaTime);
        }
    }
    //Hàm kiểm tra vật cản bằng 3 tia Raycast
    void CheckObstacle()
    {
        // Điểm bắt đầu của Raycast (cao hơn mặt đất 1m)
        Vector3 origin = transform.position + Vector3.up;

        // Hướng trước, trái và phải
        Vector3 forward = transform.forward;
        Vector3 left = Quaternion.Euler(0, -rayAngle, 0) * forward;
        Vector3 right = Quaternion.Euler(0, rayAngle, 0) * forward;

        // Bắn 3 tia Raycast
        bool hitCenter = Physics.Raycast(origin, forward, rayDistance, obstacleLayer);
        bool hitLeft = Physics.Raycast(origin, left, rayDistance, obstacleLayer);
        bool hitRight = Physics.Raycast(origin, right, rayDistance, obstacleLayer);

        // Nếu phía trước không có vật cản thì tiếp tục đi
        if (!hitCenter)
            return;

        // Ưu tiên rẽ trái
        if (!hitLeft)
        {
            Vector3 target = transform.position + left * 20f;
            SetDestination(target);
        }
        // Sau đó mới rẽ phải
        else if (!hitRight)
        {
            Vector3 target = transform.position + right * 20f;
            SetDestination(target);
        }
        // Bị chặn cả ba hướng
        else
        {
            PickRandomDestination();
        }
    }
    //Hàm để thiết lập điểm mới trên NavMesh
    void SetDestination(Vector3 point)
    {
        // Chuyển điểm bất kỳ thành điểm hợp lệ trên NavMesh
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 20f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    //Hàm chọn điểm đến ngẫu nhiên trên NavMesh
    void PickRandomDestination()
    {
        // Thử tối đa 20 lần để tìm điểm hợp lệ trên NavMesh
        for (int i = 0; i < 20; i++)
        {
            // Sinh điểm ngẫu nhiên quanh vị trí hiện tại
            Vector3 random = transform.position + Random.insideUnitSphere * moveRadius;

            // Giữ nguyên độ cao
            random.y = transform.position.y;

            // Nếu tìm được vị trí hợp lệ trên NavMesh thì di chuyển tới đó
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, moveRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        // Vẽ 3 tia Raycast trong Scene View để dễ quan sát
        Vector3 origin = transform.position + Vector3.up;

        //Tia giữa
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, transform.forward * rayDistance);

        //Tia trái
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, Quaternion.Euler(0, -rayAngle, 0) * transform.forward * rayDistance);

        //Tia phải
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin, Quaternion.Euler(0, rayAngle, 0) * transform.forward * rayDistance);
    }
}
