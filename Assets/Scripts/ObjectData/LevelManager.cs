using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public LevelData currentLevel;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnObjects(SpawnArea area)
    {
        AreaData areaData = currentLevel.areas.Find(x => x.areaType == area.areaType);

        if (areaData == null) return;

        foreach (SpawnData data in areaData.spawnDatas)
        {
            if (data.spawnArea != area.areaType) continue;

            List<SpawnPoints> availablePoints =
                new List<SpawnPoints>(area.spawnPoints);

            for (int i = 0; i < data.maxAmount; i++)
            {
                if (availablePoints.Count == 0)
                    break;

                int index = Random.Range(0, availablePoints.Count);

                SpawnPoints point = availablePoints[index];

                availablePoints.RemoveAt(index);

                Instantiate(
                    data.objectData.objectPrefab,
                    point.transform.position,
                    Quaternion.identity
                );
            }
        }
    }
}
