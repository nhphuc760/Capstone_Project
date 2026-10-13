using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum ZoneType
{
    Green,
    Yellow,
    Red
}

[Serializable]
public struct SpawnableItem 
{
    public NetworkObject prefab;
    [Range(0, 100)]
    public float weight;
    public Vector3 scaleMin;
    public Vector3 scaleMax;
}


[CreateAssetMenu(fileName = "ZoneConfig", menuName = "ProcGen/Zone Configuration")]
public class ZoneSpawnConfig : ScriptableObject
{
    public ZoneType zoneType;
    public Color debugColor = Color.white;
    [Header("Poisson Radius Settings")]
    public float radius = 2.0f;
    [Header("Item weight table")]
    public List<SpawnableItem> spawnableItems = new List<SpawnableItem>();
    public NetworkObject GetRandomItem()
    {
        if(spawnableItems == null || spawnableItems.Count == 0)
        {
            Debug.LogWarning("No spawnable items defined in ZoneSpawnConfig.");
            return null;
        }
        float totalWeight = 0f;
        foreach (var item in spawnableItems)
        {
            totalWeight += item.weight;
        }
        float randomValue = UnityEngine.Random.Range(0, totalWeight);
        float currentSum = 0f;
        foreach (var i in spawnableItems)
        {
            currentSum += i.weight;
            if (randomValue <= currentSum)
            {
                return i.prefab;
            }
        }
        return spawnableItems[0].prefab;
    }
}
