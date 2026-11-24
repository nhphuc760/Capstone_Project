using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Spectator camera switcher.
/// Làm việc với VirtualCamera của các player còn sống + 1 Main Camera persistent (đã detach).
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button exitButton;          // Thoát về menu / leave (optional)

    [Header("Optional")]
    [SerializeField] private TMPro.TextMeshProUGUI playerNameText; // Hiện tên player đang xem

    private List<PlayerCameraController> availableTargets = new();
    private int currentIndex = -1;
    private bool isInitialized;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(NextCamera);
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousCamera);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnEnable()
    {
        // Mỗi lần bật UI spectator thì refresh danh sách target
        RefreshTargets();
        if (availableTargets.Count > 0)
            SwitchToIndex(0);
    }

    /// <summary>
    /// Gọi từ GameManager khi player bị loại để bắt đầu spectator mode.
    /// </summary>
    public void StartSpectatorMode()
    {
        gameObject.SetActive(true);
        RefreshTargets();

        if (availableTargets.Count > 0)
            SwitchToIndex(0);
        else
            Debug.LogWarning("[CameraSwitcher] Không còn player nào để spectate.");
    }

    

    

    /// <summary>
    /// Làm mới danh sách player còn sống (có VirtualCamera).
    /// </summary>
    public void RefreshTargets()
    {
        availableTargets.Clear();

        // Tìm tất cả PlayerCameraController còn tồn tại trong scene
        var all = FindObjectsByType<PlayerCameraController>();

        foreach (var ctrl in all)
        {
            // Bỏ qua chính mình nếu đã bị loại (Object null hoặc không còn InputAuthority hợp lệ)
            if (ctrl == null || ctrl.Object == null || !ctrl.Object.IsValid)
                continue;

            // Chỉ lấy player còn sống (có thể thêm điều kiện IsAlive nếu có)
            availableTargets.Add(ctrl);
        }

        // Sắp xếp theo PlayerRef cho ổn định
        availableTargets = availableTargets
            .OrderBy(c => c.Object.InputAuthority.RawEncoded)
            .ToList();

        isInitialized = true;

        // Clamp index
        if (currentIndex >= availableTargets.Count)
            currentIndex = availableTargets.Count > 0 ? 0 : -1;
    }

    public void NextCamera()
    {
        if (!isInitialized) RefreshTargets();
        if (availableTargets.Count == 0) return;

        int next = (currentIndex + 1) % availableTargets.Count;
        SwitchToIndex(next);
    }

    public void PreviousCamera()
    {
        if (!isInitialized) RefreshTargets();
        if (availableTargets.Count == 0) return;

        int prev = (currentIndex - 1 + availableTargets.Count) % availableTargets.Count;
        SwitchToIndex(prev);
    }

    private void SwitchToIndex(int index)
    {
        if (availableTargets.Count == 0)
        {
            currentIndex = -1;
            return;
        }

        index = Mathf.Clamp(index, 0, availableTargets.Count - 1);

        // Tắt VirtualCamera của target cũ
        if (currentIndex >= 0 && currentIndex < availableTargets.Count)
        {
            var old = availableTargets[currentIndex];
            if (old != null)
                old.DisableForSpectator();
        }

        currentIndex = index;
        var target = availableTargets[currentIndex];

        if (target != null)
        {
            // Bật VirtualCamera của player đang spectate
            target.EnableForSpectator();

            // Cập nhật UI tên (nếu có)
            if (playerNameText != null)
            {
                playerNameText.text = $"Spectating: Player {target.Object.InputAuthority}";
            }

            Debug.Log($"[CameraSwitcher] Switching to Player {target.Object.InputAuthority}");
        }
    }

    private void OnExitClicked()
    {
        // Bạn tự hook logic thoát (leave room / về menu)
        Debug.Log("[CameraSwitcher] Exit clicked – hook logic leave room ở đây.");
        // Ví dụ: Runner.Shutdown(); hoặc SceneManager.LoadScene(...);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextCamera);
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PreviousCamera);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
    }

    // Gọi khi có player khác chết / rời để cập nhật list
    public void OnPlayerLeftOrDied(PlayerRef player)
    {
        RefreshTargets();

        if (availableTargets.Count == 0)
        {
            // Không còn ai để xem → có thể hiện UI "No one left"
            return;
        }

        // Nếu đang xem player vừa chết thì nhảy sang người khác
        if (currentIndex < 0 || currentIndex >= availableTargets.Count)
            SwitchToIndex(0);
    }
}
