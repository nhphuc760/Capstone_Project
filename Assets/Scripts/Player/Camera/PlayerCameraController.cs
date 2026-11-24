
using UnityEngine;
using Cinemachine;
using Fusion;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private CinemachineConfiner confiner3D;

    [Header("Camera Settings")]
    [SerializeField] private float defaultOrthoSize = 8f;
    [SerializeField] private Transform followTarget;

    private Coroutine moveRoutine;
    private UniTaskCompletionSource moveTcs;
    private CancellationTokenRegistration cancellationRegistration;

    private bool isSpectating;

    #region Network Lifecycle

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            DisableForSpectator();
            return;
        }

        SetupLocalCamera();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        CancelCurrentMove();

        if (virtualCamera != null)
        {
            virtualCamera.Follow = null;
            virtualCamera.enabled = false;
        }

        isSpectating = false;
    }

    private void SetupLocalCamera()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("[PlayerCamera] Virtual Camera chưa được gán!");
            return;
        }

        virtualCamera.enabled = true;
        virtualCamera.Follow = GetCameraFollowTarget();

        var lens = virtualCamera.m_Lens;
        lens.OrthographicSize = defaultOrthoSize;
        virtualCamera.m_Lens = lens;

        SetCameraBounds();
    }

    private Transform GetCameraFollowTarget()
    {
        return followTarget != null ? followTarget : transform.root;
    }

    #endregion

    #region Spectator

    /// <summary>
    /// Bật camera của Player để Spectator có thể quan sát.
    /// </summary>
    public void EnableForSpectator()
    {
        if (virtualCamera == null)
            return;

        CancelCurrentMove();

        isSpectating = true;

        virtualCamera.Follow = GetCameraFollowTarget();
        virtualCamera.enabled = true;

        SetCameraBounds();
    }

    /// <summary>
    /// Tắt camera của Player khi chuyển sang Player khác.
    /// </summary>
    public void DisableForSpectator()
    {
        if (virtualCamera == null)
            return;

        CancelCurrentMove();

        isSpectating = false;

        virtualCamera.enabled = false;
    }

    #endregion

    #region Camera Movement

    /// <summary>
    /// Di chuyển Follow Target theo World X/Z.
    /// Khóa Y theo vị trí ban đầu.
    /// </summary>
    public UniTask MoveToViewAsync(
        Vector3 targetPosition,
        float transitionDuration = 1f,
        CancellationToken cancellationToken = default)
    {
        if (followTarget == null)
        {
            Debug.LogWarning("[PlayerCamera] followTarget chưa được gán!");
            return UniTask.CompletedTask;
        }

        if (virtualCamera == null)
            return UniTask.CompletedTask;

        if (cancellationToken.IsCancellationRequested)
            return UniTask.FromCanceled(cancellationToken);

        CancelCurrentMove();

        virtualCamera.Follow = followTarget;

        Vector3 currentPosition = followTarget.position;

        // Chỉ di chuyển X/Z, giữ nguyên Y
        targetPosition.y = currentPosition.y;

        moveTcs = new UniTaskCompletionSource();

        if (transitionDuration <= 0f)
        {
            followTarget.position = targetPosition;

            moveTcs.TrySetResult();

            UniTask task = moveTcs.Task;
            moveTcs = null;

            return task;
        }

        moveRoutine = StartCoroutine(
            MoveRoutine(
                currentPosition,
                targetPosition,
                transitionDuration
            )
        );

        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(
                CancelCurrentMove
            );
        }

        return moveTcs.Task;
    }


    public Camera DetachMainCameraForSpectator()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[PlayerCamera] Không tìm thấy Main Camera!");
            return null;
        }

        Transform cameraTransform = mainCamera.transform;

        // Tách khỏi hierarchy của Player
        cameraTransform.SetParent(null, true);

        Debug.Log("[PlayerCamera] Main Camera detached for Spectator.");
        return mainCamera;

    }

    /// <summary>
    /// Fire-and-forget version.
    /// </summary>
    public void MoveToView(
        Vector3 targetPosition,
        float transitionDuration = 1f)
    {
        MoveToViewAsync(targetPosition, transitionDuration).Forget();
    }

    private IEnumerator MoveRoutine(
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            // Lock Y
            position.y = startPosition.y;

            followTarget.position = position;

            yield return null;
        }

        followTarget.position = targetPosition;

        moveRoutine = null;

        CompleteMove();
    }

    private void CompleteMove()
    {
        cancellationRegistration.Dispose();

        moveTcs?.TrySetResult();
        moveTcs = null;
    }

    private void CancelCurrentMove()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        cancellationRegistration.Dispose();

        moveTcs?.TrySetCanceled();
        moveTcs = null;
    }

    #endregion

    #region Camera Settings

    public void SetOrthoSize(float size)
    {
        if (virtualCamera == null)
            return;

        var lens = virtualCamera.m_Lens;
        lens.OrthographicSize = size;
        virtualCamera.m_Lens = lens;
    }

    public void SetCameraBounds(Collider boundsCollider = null)
    {
        if (confiner3D == null)
            return;

        if (boundsCollider == null)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag("Map");

            if (mapObject == null || mapObject.transform.childCount == 0)
            {
                Debug.LogWarning("[PlayerCamera] Không tìm thấy Map Bounds!");
                return;
            }

            boundsCollider = mapObject.transform
                .GetChild(0)
                .GetComponent<BoxCollider>();
        }

        if (boundsCollider == null)
            return;

        confiner3D.m_BoundingVolume = boundsCollider;

        confiner3D.InvalidatePathCache();
    }

    public void SetFollowTarget(Transform target)
    {
        if (virtualCamera != null)
            virtualCamera.Follow = target;
    }

    #endregion
}
