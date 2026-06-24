using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader: MonoBehaviour
{
    [SerializeField] Image loadingBar;
    [SerializeField] float fillSpeed = .5f;
    [SerializeField] Canvas loadingCanvas;
    [SerializeField] Camera loadingCamera;
    [SerializeField] SceneGroup[] sceneGroups;
    float targetProgress;
    bool isLoading;
    public readonly SceneGroupManager manager = new SceneGroupManager();
    async void Start()
    {
        await LoadSceneGroup(0);
    }
    public async UniTask LoadSceneGroup(int index)
    {
        loadingBar.fillAmount = 0f;
        targetProgress = 1f;
        if(index < 0 || index >= sceneGroups.Length)
        {
            Debug.LogError("Invalid scene group index");
            return;
        }
        LoadingProgress progress = new LoadingProgress();
        progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);

    }
}
public class LoadingProgress : IProgress<float>
{
    public event Action<float> Progressed;
    const float ratio = 1f;
    public void Report(float value)
    {
        Progressed?.Invoke(value/ratio);
    }
}
