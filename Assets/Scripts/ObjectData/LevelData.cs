using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AreaData
{
    public SpawnAreaType areaType;

    public List<SpawnData> spawnDatas = new();
}

public enum levelname
{
    Tutorial,
    Level1,
    Level2,
    Level3
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Information")]
    public levelname levelName;

    [Header("Score")]
    public PointData pointData;

    [Header("Areas")]
    public List<AreaData> areas = new();
}