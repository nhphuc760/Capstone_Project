using UnityEngine;

public enum SpawnAreaType
{
    House,
    Church,
    Warehouse,
    Forest,
    Cemetery
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "Game/Spawn Data")]
public class SpawnData : ScriptableObject
{
    [Header("Object")]
    public ObjectData objectData;

    [Header("Spawn")]
    public SpawnAreaType spawnArea;

    // Random minimum and maximum amount of objects to spawn in the spawn area
    [Min(1)]
    public int minAmount;

    [Min(1)]
    public int maxAmount;

    [Range(0,100)]
    public int spawnChance; // Chance of spawning this object in the spawn area (0-100%)
}