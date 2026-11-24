using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogoutButtonHandler : MonoBehaviour
{
    [Header("Tên Scene Main Menu / Login quay về")]
    [SerializeField] private string mainMenuSceneName = "MainMenuXin";

    [Header("Nút bấm (Tự lấy nếu gắn cùng object)")]
    [SerializeField] private Button logoutButton;

    private bool isLoggingOut;

    private void Awake()
    {
        if (logoutButton == null)
            logoutButton = GetComponent<Button>();

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnClickLogout);
    }

    private void OnDestroy()
    {
        if (logoutButton != null)
            logoutButton.onClick.RemoveListener(OnClickLogout);
    }

    public void OnClickLogout()
    {
        if (isLoggingOut) return;
        isLoggingOut = true;

        if (logoutButton != null)
            logoutButton.interactable = false;

        GameObject runnerObj = new GameObject("LogoutTransitionRunner");
        DontDestroyOnLoad(runnerObj);
        LogoutTransitionRunner runner = runnerObj.AddComponent<LogoutTransitionRunner>();
        runner.ExecuteLogout(mainMenuSceneName).Forget();
    }

    private sealed class LogoutTransitionRunner : MonoBehaviour
    {
        public async UniTaskVoid ExecuteLogout(string targetSceneName)
        {
            try
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // 1. Che màn hình chuyển cảnh bằng SceneController gốc
                if (SceneController.Instance != null)
                {
                    await SceneController.Instance.NewTransitionPlan()
                        .WithFadeIn()
                        .Perform();
                }

                FirebaseAuth auth = FirebaseAuth.DefaultInstance;
                FirebaseUser currentUser = auth.CurrentUser;

                // 2. Trả trạng thái Firebase về Offline TRƯỚC khi SignOut
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.UserId))
                {
                    try
                    {
                        await FirebaseDatabase.DefaultInstance
                            .GetReference($"Users/{currentUser.UserId}/Presence/Status")
                            .SetValueAsync((int)OnlineStatus.Offline);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Logout] Không cập nhật được trạng thái Offline: {ex.Message}");
                    }
                }

                // 3. Đăng xuất khỏi Firebase Auth
                auth.SignOut();

                // 4. Chỉ xóa các manager của phiên cũ trong DontDestroyOnLoad (GIỮ LẠI SceneController & Firebase)
                CleanupSessionOnlyDontDestroyObjects();
                await UniTask.Yield();

                // 5. Tải lại MainMenuXin ở chế độ Single
                await SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
                await UniTask.Yield();

                // 6. Xóa bản sao SceneController / EventSystem / AudioListener trong MainMenuXin và bung Layout
                DeduplicateSceneComponentsAndRebuildLayout();

                // 7. Mở màn hình chuyển cảnh bằng SceneController gốc
                if (SceneController.Instance != null)
                {
                    await SceneController.Instance.NewTransitionPlan()
                        .WithFadeOut()
                        .Perform();

                    UnblockSceneControllerOverlayRaycasts();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Logout] Lỗi khi đăng xuất: {ex.Message}");
            }
            finally
            {
                Destroy(gameObject);
            }
        }

        private void CleanupSessionOnlyDontDestroyObjects()
        {
            Scene ddolScene = gameObject.scene;
            GameObject keepSceneController = SceneController.Instance != null
                ? SceneController.Instance.transform.root.gameObject
                : null;

            foreach (GameObject rootObj in ddolScene.GetRootGameObjects())
            {
                if (rootObj == null || rootObj == gameObject) continue;
                if (rootObj == keepSceneController) continue;
                if (rootObj.name.Contains("DOTween")) continue;
                if (rootObj.name.Contains("Firebase")) continue;
                if (rootObj.name.Contains("Debug Updater")) continue;

                // Xóa NetworkDataManager, RoomDatabaseManager, ObjectPoolManager của tài khoản cũ
                Destroy(rootObj);
            }
        }

        private void DeduplicateSceneComponentsAndRebuildLayout()
        {
            SceneController primaryController = SceneController.Instance;

            // Xóa bản sao SceneController nằm sẵn trong MainMenuXin để tránh trùng LoadingOverlay & EventSystem
            SceneController[] controllers = FindObjectsByType<SceneController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SceneController sc in controllers)
            {
                if (sc == null || sc == primaryController) continue;
                Destroy(sc.gameObject);
            }

            // Giữ lại duy nhất 1 EventSystem (ưu tiên cái thuộc SceneController gốc)
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem primaryEventSystem = primaryController != null
                ? primaryController.GetComponentInChildren<EventSystem>(true)
                : null;

            if (primaryEventSystem == null && eventSystems.Length > 0)
            {
                primaryEventSystem = eventSystems[0];
            }

            foreach (EventSystem es in eventSystems)
            {
                if (es == null || es == primaryEventSystem) continue;
                es.enabled = false;
                Destroy(es.gameObject);
            }

            if (primaryEventSystem != null)
            {
                primaryEventSystem.gameObject.SetActive(true);
                primaryEventSystem.enabled = false;
                primaryEventSystem.enabled = true;
            }

            // Giữ lại duy nhất 1 AudioListener
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioListener primaryListener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
            if (primaryListener == null && listeners.Length > 0)
            {
                primaryListener = listeners[0];
            }

            foreach (AudioListener listener in listeners)
            {
                if (listener == null || listener == primaryListener) continue;
                listener.enabled = false;
                Destroy(listener);
            }

            if (primaryListener != null)
            {
                primaryListener.enabled = true;
            }

            // Ép Canvas & Vertical Layout Group tính lại vị trí 3 nút menu
            Canvas.ForceUpdateCanvases();
            LayoutGroup[] layoutGroups = FindObjectsByType<LayoutGroup>(FindObjectsSortMode.None);
            foreach (LayoutGroup lg in layoutGroups)
            {
                if (lg != null && lg.transform is RectTransform rect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
            }
        }

        private void UnblockSceneControllerOverlayRaycasts()
        {
            if (SceneController.Instance == null) return;

            foreach (CanvasGroup cg in SceneController.Instance.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (cg.alpha <= 0.05f)
                    cg.blocksRaycasts = false;
            }

            foreach (Image img in SceneController.Instance.GetComponentsInChildren<Image>(true))
            {
                if (img.color.a <= 0.05f)
                    img.raycastTarget = false;
            }
        }
    }
}