
using System.Collections.Generic;
using UnityEngine;

public class SuggestTarget : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float searchRadius = 2.5f;

    [SerializeField]
    private LayerMask targetLayers = ~0;

    [SerializeField, Min(0.02f)]
    private float searchInterval = 0.15f;

    private readonly List<Targetable> targets = new();
    private readonly HashSet<Targetable> uniqueTargets = new();

    private Targetable currentTarget;
    private float searchTimer;
    public Targetable CurrentTarget => currentTarget;

    private void Start()
    {
        SetTarget(null);
        RefreshTargets();
        SelectNearestTarget();
    }

    private void Update()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            searchTimer = searchInterval;
            RefreshTargets();
            ValidateCurrentTarget();
        }

        // Nhấn Tab để chuyển sang target tiếp theo.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleTarget();
        }
    }

    private void RefreshTargets()
    {        
        targets.Clear();
        uniqueTargets.Clear();

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in colliders)
        {
            Targetable target =
                col.GetComponentInParent<Targetable>();

            if (target == null ||
                !target.CanBeTargeted ||
                !target.gameObject.activeInHierarchy ||
                target.transform == transform ||
                !uniqueTargets.Add(target))
            {
                continue;
            }

            targets.Add(target);
        }

        // Sắp xếp target gần nhất lên đầu danh sách.
        targets.Sort((a, b) =>
        {
            float distanceA = (
                a.TargetTransform.position - transform.position
            ).sqrMagnitude;

            float distanceB = (
                b.TargetTransform.position - transform.position
            ).sqrMagnitude;

            return distanceA.CompareTo(distanceB);
        });
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null ||
            !currentTarget.CanBeTargeted ||
            !targets.Contains(currentTarget))
        {
            SelectNearestTarget();
        }
    }

    private void SelectNearestTarget()
    {
        SetTarget(targets.Count > 0 ? targets[0] : null);
    }

    private void CycleTarget()
    {
        // Cập nhật ngay để tránh dùng danh sách cũ.
        RefreshTargets();

        if (targets.Count == 0)
        {
            SetTarget(null);
            return;
        }

        int currentIndex = targets.IndexOf(currentTarget);

        // Chưa có target hoặc target cũ đã ra khỏi range.
        if (currentIndex < 0)
        {
            SelectNearestTarget();
            return;
        }

        // Chuyển sang đối tượng tiếp theo, quay vòng về đầu.
        int nextIndex = (currentIndex + 1) % targets.Count;
        SetTarget(targets[nextIndex]);
    }

    private void SetTarget(Targetable target)
    {
        //Clear curTarget
        if (currentTarget)
        {
            currentTarget.SetActiveIndicator(false);
        }
        //Set newTarget
        currentTarget = target;

        if(target != null)
        {
            target.SetActiveIndicator(true);
        }
    }

   

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}
