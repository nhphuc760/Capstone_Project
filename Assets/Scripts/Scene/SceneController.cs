using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public LoadingOverlay loadingOverlay;
    List<string> loadedSceneBySlot = new();
    public static SceneController Instance { get; private set; }
    public bool isBusy;
    public bool DontDestroy;

    private Camera fallbackCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (DontDestroy)
        {
            DontDestroyOnLoad(gameObject);
            CreateFallbackCamera();
        }
        loadedSceneBySlot.Add(SceneManager.GetActiveScene().name);
    }

    private void CreateFallbackCamera()
    {
        GameObject camObj = new GameObject("FallbackBlackCamera");
        camObj.transform.SetParent(transform);
        fallbackCam = camObj.AddComponent<Camera>();
        fallbackCam.clearFlags = CameraClearFlags.SolidColor;
        fallbackCam.backgroundColor = Color.black;
        fallbackCam.cullingMask = 0; // Chỉ giữ nền đen sạch, không render vật thể
        fallbackCam.depth = -100;
        fallbackCam.enabled = false;
    }

    private void LateUpdate()
    {
        if (fallbackCam == null) return;

        // Tự bật camera nền đen nếu toàn bộ các scene hiện tại không có camera nào đang chạy
        int otherActiveCameras = fallbackCam.enabled ? Camera.allCamerasCount - 1 : Camera.allCamerasCount;
        fallbackCam.enabled = (otherActiveCameras <= 0);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            loadedSceneBySlot.Clear();
            loadedSceneBySlot.Add(scene.name);
        }
    }

    public SceneTransitionPlan NewTransitionPlan()
    {
        return new SceneTransitionPlan();
    }

    public async UniTask ExecutePlan(SceneTransitionPlan plan)
    {
        if (isBusy) return;
        isBusy = true;
        await ChangeSceneAsync(plan);
    }

    public async UniTask ChangeSceneAsync(SceneTransitionPlan plan)
    {
        if (plan.FadeIn)
        {
            await loadingOverlay.FadeInBlack(.5f);
        }
        // Load new scenes cho mượt
        foreach (var scene in plan.SceneToLoad)
        {
            if (loadedSceneBySlot.Contains(scene.Name))
            {
                await UnloadSceneAsync(scene);
            }
            await LoadAdditiveAsync(scene, scene.Name == plan.ActiveSceneName);
        }
        foreach (var scene in plan.SceneToUnload)
        {
            await UnloadSceneAsync(scene);
        }

        if (plan.ClearUnuseAssets)
        {
            await CleanUnusedAssetsAsync();
        }

        if (plan.FadeOut)
        {
            await loadingOverlay.FadeOutBlack(.5f);
        }
        isBusy = false;
    }

    async UniTask LoadAdditiveAsync(ParameterScene param, bool setActive)
    {
        if (param.onBeforeExecute != null)
            await param.onBeforeExecute();
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(param.Name, LoadSceneMode.Additive);
        if (loadOp == null)
        {
            return;
        }
        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f)
        {
            await UniTask.Yield();
        }
        loadOp.allowSceneActivation = true;
        await UniTask.WaitUntil(() => loadOp.isDone);
        if (setActive)
        {
            Scene newScene = SceneManager.GetSceneByName(param.Name);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }
        loadedSceneBySlot.Add(param.Name);
    }

    async UniTask UnloadSceneAsync(ParameterScene param)
    {
        if (!loadedSceneBySlot.Contains(param.Name)) return;
        if (string.IsNullOrEmpty(param.Name)) return;
        if (param.onBeforeExecute != null)
            await param.onBeforeExecute();
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(param.Name);
        if (unloadOp != null)
        {
            await UniTask.WaitUntil(() => unloadOp.isDone);
        }
        loadedSceneBySlot.Remove(param.Name);
    }

    async UniTask CleanUnusedAssetsAsync()
    {
        var op = Resources.UnloadUnusedAssets();
        await UniTask.WaitUntil(() => op.isDone);
    }
}

public class SceneTransitionPlan
{
    public List<ParameterScene> SceneToLoad = new();
    public List<ParameterScene> SceneToUnload = new();
    public string ActiveSceneName { get; private set; }
    public string progressTitle { get; private set; }
    public bool ClearUnuseAssets { get; private set; } = false;
    public bool FadeIn { get; private set; }
    public bool FadeOut { get; private set; }

    public SceneTransitionPlan Load(ParameterScene sceneParam, bool isActiveScene = false)
    {
        if (SceneToLoad.Contains(sceneParam)) return this;
        SceneToLoad.Add(sceneParam);
        if (isActiveScene)
            ActiveSceneName = sceneParam.Name;
        return this;
    }

    public SceneTransitionPlan UnLoad(ParameterScene sceneParam)
    {
        if (SceneToUnload.Contains(sceneParam)) return this;
        SceneToUnload.Add(sceneParam);
        return this;
    }

    public SceneTransitionPlan WithClearUnuseAssets()
    {
        ClearUnuseAssets = true;
        return this;
    }

    public SceneTransitionPlan WithFadeIn()
    {
        FadeIn = true;
        return this;
    }

    public SceneTransitionPlan WithFadeOut()
    {
        FadeOut = true;
        return this;
    }

    public async UniTask Perform()
    {
        await SceneController.Instance.ExecutePlan(this);
    }
}

public struct ParameterScene : IEquatable<ParameterScene>
{
    public string Name;
    public Func<UniTask> onBeforeExecute;

    public bool Equals(ParameterScene other)
    {
        return Name == other.Name
        && onBeforeExecute == other.onBeforeExecute;
    }

    public override bool Equals(object obj)
    {
        return obj is ParameterScene other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            onBeforeExecute);
    }

    public static bool operator ==(
       ParameterScene left,
       ParameterScene right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        ParameterScene left,
        ParameterScene right)
    {
        return !left.Equals(right);
    }
}