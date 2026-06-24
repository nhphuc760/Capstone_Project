using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;
using UnityEngine;

public enum SceneType 
{
    ActiveScene,
    MainMenu,
    HUD,
    Cinematic,
    Environment,
    Tooling
}

[Serializable]

public class SceneGroup
{
    public string GroupName = "New Scene Group"; // as a title loading
    public List<SceneData> Scenes;
    public string FindSceneNameByType(SceneType sceneType)
    {
        return Scenes.FirstOrDefault(scene => scene.SceneType == sceneType)?.Reference.Name;
    }
}

[Serializable]
public class SceneData 
{
    public SceneReference Reference;
    public string Name => Reference.Name;
    public SceneType SceneType;
}
