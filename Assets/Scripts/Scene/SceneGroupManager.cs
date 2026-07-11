using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGroupManager 
{
    public event Action<string> OnSceneLoaded = delegate { };
    public event Action<string> OnSceneUnloaded = delegate { };
    public event Action OnSceneGoupLoaded = delegate { };
    SceneGroup ActiveSceneGoup;
    public async UniTask LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
    {
        ActiveSceneGoup = group;
        var loadedScenes = new List<string>();
        await UnLoadScenes();
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i ++)
        {
            loadedScenes.Add(SceneManager.GetSceneAt(i).name);
        }
        var totalSceneToLoad = ActiveSceneGoup.Scenes.Count;
        var operationGroup = new AsyncOperationGroup(totalSceneToLoad);
        for (var i = 0; i< totalSceneToLoad; i++)
        {
            var sceneData = group.Scenes[i];
            if (reloadDupScenes == false && loadedScenes.Contains(sceneData.Name)) continue;
            var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
            operationGroup.Operations.Add(operation);
            OnSceneLoaded.Invoke(sceneData.Name);
        }
        while (!operationGroup.IsDone)
        {
            progress?.Report(operationGroup.Progress);
            await UniTask.Delay(100);
        }
        Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGoup.FindSceneNameByType(SceneType.ActiveScene));
        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
        }
        OnSceneGoupLoaded?.Invoke();
    }
    public async UniTask UnLoadScenes()
    {
        var scenes = new List<string>();
        var activeScene = SceneManager.GetActiveScene().name;
        int sceneCount = SceneManager.sceneCount;
        for (int i = sceneCount - 1; i >= 0; i--)
        {
            var sceneAt = SceneManager.GetSceneAt(i);
            if(!sceneAt.isLoaded) continue;
            var sceneName = sceneAt.name;
            if(sceneName.Equals(activeScene) || sceneName == "Bootstrapper") continue;
            scenes.Add(sceneName);
        }
        var operationGoups = new AsyncOperationGroup(scenes.Count);
        foreach (var scene in scenes)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            if(operation == null) continue;
            operationGoups.Operations.Add(operation);
            OnSceneUnloaded?.Invoke(scene);
        }
        await UniTask.WaitUntil(() => operationGoups.IsDone);
        await Resources.UnloadUnusedAssets();
    }
}
public readonly struct AsyncOperationGroup 
{
    public readonly List<AsyncOperation> Operations;
    public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
    public bool IsDone => Operations.All(o => o.isDone);
    public AsyncOperationGroup(int initialCapacity)
    {
        Operations = new List<AsyncOperation>(initialCapacity);
    }
}