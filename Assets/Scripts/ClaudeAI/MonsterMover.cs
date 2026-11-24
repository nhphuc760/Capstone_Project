using UnityEngine;
using UnityEngine.AI;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Bọc NavMeshAgent. Chỉ bật trên host (state authority); client nhận vị trí qua NetworkTransform.
    /// Muốn quái bay / không dùng NavMesh: viết lớp mover khác với cùng API.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterMover : MonoBehaviour
    {
        NavMeshAgent _agent;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.autoBraking = false;
            _agent.angularSpeed = 540f;
            _agent.acceleration = 40f;
        }

        public void SetAuthority(bool hasAuthority) => _agent.enabled = hasAuthority;

        bool Ready => _agent.enabled && _agent.isOnNavMesh;

        public void SetSpeed(float speed) { if (_agent.enabled) _agent.speed = speed; }

        public void MoveTo(Vector3 position)
        {
            if (!Ready) return;
            _agent.isStopped = false;
            _agent.SetDestination(position);
        }

        public void Stop()
        {
            if (!Ready) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public bool HasArrived(float tolerance)
        {
            if (!Ready || _agent.pathPending) return false;
            return _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, tolerance);
        }

        public void Face(Vector3 target, float dt)
        {
            Vector3 d = target - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 540f * dt);
        }
    }
}
