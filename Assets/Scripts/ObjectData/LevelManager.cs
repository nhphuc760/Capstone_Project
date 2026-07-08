using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Current Level")]
    public LevelData currentLevel;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnObjects(SpawnArea area)
{
    AreaData areaData = currentLevel.areas.Find(x => x.areaType == area.areaType);

    if (areaData == null)
        return;

    foreach (SpawnData data in areaData.spawnDatas)
    {
        if (Random.Range(0, 100) >= data.spawnChance)
            continue;

        int amount = Random.Range(data.minAmount, data.maxAmount + 1);

        for (int i = 0; i < amount; i++)
        {
            SpawnPoints point = GetRandomSpawnPoint(area);

            if (point == null)
                break;

            GameObject obj = Instantiate(
                data.objectData.objectPrefab,
                point.GetSpawnPosition(),
                Quaternion.identity
            );

            point.SetOccupied(true);
        }
    }
}

    private SpawnPoints GetRandomSpawnPoint(SpawnArea area)
{
    List<SpawnPoints> available = new();

    foreach (SpawnPoints point in area.spawnPoints)
    {
        if (!point.canSpawn)
            continue;

        if (point.occupied)
            continue;

        available.Add(point);
    }

    if (available.Count == 0)
        return null;

    return available[Random.Range(0, available.Count)];
}
}