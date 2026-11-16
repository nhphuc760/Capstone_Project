using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SuggestTarget : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] private float searchRadius = 2.5f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Min(0.02f)] private float searchInterval = 0.15f;

    // === Networked State ===
    [Networked, OnChangedRender(nameof(OnTargetChanged))]
    public NetworkId CurrentTargetId { get; set; }

    // Local cache
    private readonly List<Targetable> _targets = new();
    private readonly HashSet<Targetable> _uniqueTargets = new();
    private Targetable _localCurrentTarget;          // cache để tránh lookup liên tục
    private float _searchTimer;

    public Targetable CurrentTarget => _localCurrentTarget;

    public override void Spawned()
    {
        // Khi object vừa spawn, đảm bảo indicator tắt
        if (HasInputAuthority)
        {
            SetLocalIndicator(null);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Cleanup indicator khi object bị despawn
        if (HasInputAuthority && _localCurrentTarget != null)
        {
            _localCurrentTarget.SetActiveIndicator(false);
            _localCurrentTarget = null;
        }
    }

    private void Update()
    {
        // Chỉ người có InputAuthority mới detect + nhận input
        if (!HasInputAuthority) return;

        _searchTimer -= Time.deltaTime;

        if (_searchTimer <= 0f)
        {
            _searchTimer = searchInterval;
            RefreshTargets();
            ValidateCurrentTarget();
        }

        // Tab để cycle target
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleTarget();
        }
    }

    // ======================== Detection ========================

    private void RefreshTargets()
    {
        _targets.Clear();
        _uniqueTargets.Clear();

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (var col in colliders)
        {
            Targetable target = col.GetComponentInParent<Targetable>();

            if (target == null ||
                !target.CanBeTargeted ||
                !target.gameObject.activeInHierarchy ||
                target.transform == transform ||
                !_uniqueTargets.Add(target))
            {
                continue;
            }

            // Chỉ lấy những object có NetworkObject (để sync được)
            if (target.GetComponentInParent<NetworkObject>() == null)
                continue;

            _targets.Add(target);
        }

        // Sort theo khoảng cách gần → xa
        _targets.Sort((a, b) =>
        {
            float distA = (a.TargetTransform.position - transform.position).sqrMagnitude;
            float distB = (b.TargetTransform.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });
    }

    private void ValidateCurrentTarget()
    {
        // Nếu target hiện tại không còn hợp lệ → chọn lại target gần nhất
        if (_localCurrentTarget == null ||
            !_localCurrentTarget.CanBeTargeted ||
            !_targets.Contains(_localCurrentTarget))
        {
            SelectNearestTarget();
        }
    }

    private void SelectNearestTarget()
    {
        if (_targets.Count > 0)
        {
            RequestSetTarget(_targets[0]);
        }
        else
        {
            RequestSetTarget(null);
        }
    }

    private void CycleTarget()
    {
        // Luôn refresh trước khi cycle để danh sách mới nhất
        RefreshTargets();

        if (_targets.Count == 0)
        {
            RequestSetTarget(null);
            return;
        }

        int currentIndex = _localCurrentTarget != null
            ? _targets.IndexOf(_localCurrentTarget)
            : -1;

        // Không tìm thấy hoặc chưa có target → lấy cái gần nhất
        if (currentIndex < 0)
        {
            RequestSetTarget(_targets[0]);
            return;
        }

        // Cycle sang target tiếp theo (quay vòng)
        int nextIndex = (currentIndex + 1) % _targets.Count;
        RequestSetTarget(_targets[nextIndex]);
    }

    // ======================== Network Request ========================

    private void RequestSetTarget(Targetable target)
    {
        NetworkId id = default;

        if (target != null)
        {
            NetworkObject netObj = target.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.Id.IsValid)
            {
                id = netObj.Id;
            }
        }

        // Nếu đang là StateAuthority thì set luôn, không cần RPC
        if (HasStateAuthority)
        {
            SetTarget(id);
        }
        else
        {
            RPC_RequestSetTarget(id);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetTarget(NetworkId targetId)
    {
        SetTarget(targetId);
    }

    // Chỉ StateAuthority mới gọi hàm này
    private void SetTarget(NetworkId targetId)
    {
        // Tránh set lại cùng giá trị (giảm bandwidth + tránh trigger OnChangedRender thừa)
        if (CurrentTargetId == targetId) return;

        CurrentTargetId = targetId;
    }

    // ======================== Visual / Local ========================

    private void OnTargetChanged()
    {
        // Chỉ InputAuthority cần update visual indicator
        if (!HasInputAuthority) return;

        Targetable newTarget = null;

        if (CurrentTargetId.IsValid &&
            Runner.TryFindObject(CurrentTargetId, out NetworkObject netObj))
        {
            newTarget = netObj.GetComponentInChildren<Targetable>();
        }

        SetLocalIndicator(newTarget);
    }

    private void SetLocalIndicator(Targetable newTarget)
    {
        // Tắt indicator cũ
        if (_localCurrentTarget != null)
        {
            _localCurrentTarget.SetActiveIndicator(false);
        }

        _localCurrentTarget = newTarget;

        // Bật indicator mới
        if (_localCurrentTarget != null)
        {
            _localCurrentTarget.SetActiveIndicator(true);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}