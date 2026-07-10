using System.Collections.Generic;
using UnityEngine;

public enum SpawnAreaType
{
    Tutorial,
    Forest,
    City,
    Cemetery
}

[System.Serializable]
public class AreaData
{
    [Header("Area")]
    public SpawnAreaType areaType;

    [Header("Spawn Objects")]
    public List<SpawnData> spawnDatas = new();
}

public enum levelname
{
    Tutorial,
    Level1,
    Level2,
    Level3
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Game Data/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Information")]
    public levelname levelName;

    [Header("Score")]
    public PointData pointData;

    [Header("Areas")]
    public List<AreaData> areas = new();

    [Header("Time")]
    public TimeData timeData;
}