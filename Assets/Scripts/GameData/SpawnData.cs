using UnityEngine;

public enum AreaState
{
    Waiting,
    Spawning,
    Active,
    Cleared
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "Game Data/Spawn Data")]
public class SpawnData : ScriptableObject
{
    [Header("Object")]
    public ObjectData objectData;

    [Header("Spawn Rule")]
    [Min(1)]
    public int minAmount = 1;

    [Min(1)]
    public int maxAmount = 3;

    [Range(0, 100)]
    public int spawnChance = 100;

    [Header("Area State")]
    public AreaState areaState = AreaState.Waiting;
}