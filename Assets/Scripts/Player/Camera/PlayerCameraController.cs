using UnityEngine;
using Cinemachine;
using Fusion;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;   // Kéo Virtual Camera trong Scene vào đây
    [SerializeField] private CinemachineConfiner confiner3D;         // Kéo component Confiner2D

    [Header("Optional")]
    [SerializeField] private float defaultOrthoSize = 8f;

    public override void Spawned()
    {
        if (!enabled || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            if (virtualCamera != null)
                virtualCamera.enabled = false;
            return;
        }
        // Chỉ local player mới được điều khiển camera
        if (HasInputAuthority == false)
        {
            // Camera của player khác thì tắt hoàn toàn
            if (virtualCamera != null)
                virtualCamera.enabled = false;
            return;
        }

        // === Local Player ===
        SetupLocalCamera();
    }

    private void SetupLocalCamera()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("[PlayerCamera] Virtual Camera chưa được gán!");
            return;
        }

        // Bật camera
        virtualCamera.enabled = true;

        // Gán Follow target
        virtualCamera.Follow = transform.root;
        // virtualCamera.LookAt = transform; // Chỉ dùng nếu cần

        // Set Ortho Size (nếu muốn)
        var lens = virtualCamera.m_Lens;
        //lens.OrthographicSize = defaultOrthoSize;
        virtualCamera.m_Lens = lens;

        // === Set Bounding Shape khi Spawned ===
        SetCameraBounds();
    }

    /// <summary>
    /// Gọi hàm này để set Bounding Shape cho Confiner
    /// Bạn có thể gọi lại sau này nếu map thay đổi
    /// </summary>
    public void SetCameraBounds(Collider boundsCollider = null)
    {
        if (!enabled) return;
        if (confiner3D == null) return;

        if (boundsCollider != null)
        {
            confiner3D.m_BoundingVolume = boundsCollider;
        }
        else
        {
            //// Tự tìm bounds trong scene (ví dụ theo tag hoặc tên)
            //var boundsObj = GameObject.FindGameObjectWithTag("Map").transform.GetChild(0).GetComponent<BoxCollider>();
            //if (boundsObj != null)
            //{
            //    confiner3D.m_BoundingVolume = boundsObj;
            //}
            // Tự tìm bounds trong scene khi sang GameplayScene
            var mapObj = GameObject.FindGameObjectWithTag("Map");
            if (mapObj != null && mapObj.transform.childCount > 0)
            {
                var boundsObj = mapObj.transform.GetChild(0).GetComponent<BoxCollider>();
                if (boundsObj != null)
                {
                    confiner3D.m_BoundingVolume = boundsObj;
                }
            }
        }

        // Force invalidate cache (rất quan trọng)
        confiner3D.InvalidatePathCache();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority && virtualCamera != null)
        {
            virtualCamera.Follow = null;
            virtualCamera.enabled = false;
        }
    }

    // Optional: Cho phép đổi Ortho Size runtime (zoom)
    public void SetOrthoSize(float size)
    {
        if (virtualCamera == null) return;

        var lens = virtualCamera.m_Lens;
        lens.OrthographicSize = size;
        virtualCamera.m_Lens = lens;
    }
}