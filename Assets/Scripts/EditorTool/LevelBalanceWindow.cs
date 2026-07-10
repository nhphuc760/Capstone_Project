using UnityEngine;
using UnityEditor;

public class LevelBalanceWindow : EditorWindow
{
    private LevelData levelData;

    private int warningCount;
    private int errorCount;

    [MenuItem("Tools/Level Balance Analyzer")]
    public static void OpenWindow()
    {
        GetWindow<LevelBalanceWindow>("Level Balance");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label("Level Balance Analyzer", EditorStyles.boldLabel);

        GUILayout.Space(5);

        levelData = (LevelData)EditorGUILayout.ObjectField(
            "Level Data",
            levelData,
            typeof(LevelData),
            false);

        GUILayout.Space(10);

        if (GUILayout.Button("Analyze Level"))
        {
            AnalyzeLevel();
        }
    }

    private void AnalyzeLevel()
    {
        warningCount = 0;
        errorCount = 0;

        if (levelData == null)
        {
            Debug.LogError("❌ Please assign a LevelData.");
            return;
        }

        if (levelData.areas == null || levelData.areas.Count == 0)
        {
            Debug.LogError("❌ Level has no Area.");
            return;
        }

        int totalMinScore = 0;
        int totalMaxScore = 0;

        float totalMinWeight = 0;
        float totalMaxWeight = 0;

        int totalMinObjects = 0;
        int totalMaxObjects = 0;

        Debug.Log("=====================================");
        Debug.Log($"LEVEL : {levelData.levelName}");
        Debug.Log("=====================================");

        foreach (AreaData area in levelData.areas)
        {
            if (area.spawnDatas == null || area.spawnDatas.Count == 0)
            {
                Warning($"Area [{area.areaType}] has no SpawnData.");
                continue;
            }

            Debug.Log($"AREA : {area.areaType}");

            foreach (SpawnData spawn in area.spawnDatas)
            {
                //----------------------------------------
                // Validation
                //----------------------------------------

                if (spawn.objectData == null)
                {
                    Error($"Area [{area.areaType}] contains SpawnData without ObjectData.");
                    continue;
                }

                if (spawn.minAmount > spawn.maxAmount)
                {
                    Error($"{spawn.objectData.objectName} : MinAmount > MaxAmount.");
                }

                if (spawn.objectData.objectPoints <= 0)
                {
                    Warning($"{spawn.objectData.objectName} has 0 Point.");
                }

                if (spawn.objectData.weight <= 0)
                {
                    Warning($"{spawn.objectData.objectName} has 0 Weight.");
                }

                if (spawn.spawnChance <= 0)
                {
                    Warning($"{spawn.objectData.objectName} has SpawnChance = 0%.");
                }

                //----------------------------------------
                // Calculate
                //----------------------------------------

                int minScore =
                    spawn.minAmount *
                    spawn.objectData.objectPoints;

                int maxScore =
                    spawn.maxAmount *
                    spawn.objectData.objectPoints;

                float minWeight =
                    spawn.minAmount *
                    spawn.objectData.weight;

                float maxWeight =
                    spawn.maxAmount *
                    spawn.objectData.weight;

                totalMinScore += minScore;
                totalMaxScore += maxScore;

                totalMinWeight += minWeight;
                totalMaxWeight += maxWeight;

                totalMinObjects += spawn.minAmount;
                totalMaxObjects += spawn.maxAmount;

                Debug.Log(
                    $"Object : {spawn.objectData.objectName}\n" +
                    $"Spawn : {spawn.minAmount} - {spawn.maxAmount}\n" +
                    $"Point : {spawn.objectData.objectPoints}\n" +
                    $"Weight : {spawn.objectData.weight}\n" +
                    $"Score : {minScore} - {maxScore}\n");
            }

            Debug.Log("-------------------------------------");
        }

        //----------------------------------------
        // Average
        //----------------------------------------

        int averageScore = (totalMinScore + totalMaxScore) / 2;

        float averageWeight =
            (totalMinWeight + totalMaxWeight) / 2f;

        int averageObjects =
            (totalMinObjects + totalMaxObjects) / 2;

        //----------------------------------------
        // Time Validation
        //----------------------------------------

        if (levelData.timeData != null)
        {
            float estimatedTime = averageObjects * 8f;

            Debug.Log($"Estimated Play Time : {estimatedTime:F1} seconds");

            if (estimatedTime > levelData.timeData.levelTime)
            {
                Warning(
                    $"Estimated play time ({estimatedTime:F1}s) exceeds Level Time ({levelData.timeData.levelTime:F1}s).");
            }
        }
        else
        {
            Warning("TimeData is missing.");
        }

        //----------------------------------------
        // PointData Validation
        //----------------------------------------

        if (levelData.pointData != null)
        {
            if (levelData.pointData.scoreB >= levelData.pointData.scoreA)
                Error("Rank B must be lower than Rank A.");

            if (levelData.pointData.scoreA >= levelData.pointData.scoreS)
                Error("Rank A must be lower than Rank S.");

            if(levelData.pointData.scoreS >= levelData.pointData.scoreSS)
                Error("Rank S must be lower than Rank SS.");

            if (totalMaxScore < levelData.pointData.scoreB)
            {
                Error("Maximum obtainable score is lower than Rank B.");
            }
        }
        else
        {
            Warning("PointData is missing.");
        }

        //----------------------------------------
        // Result
        //----------------------------------------

        Debug.Log("================ RESULT ================");

        Debug.Log($"Minimum Objects : {totalMinObjects}");
        Debug.Log($"Maximum Objects : {totalMaxObjects}");
        Debug.Log($"Average Objects : {averageObjects}");

        Debug.Log("");

        Debug.Log($"Minimum Score : {totalMinScore}");
        Debug.Log($"Maximum Score : {totalMaxScore}");
        Debug.Log($"Average Score : {averageScore}");

        Debug.Log("");

        Debug.Log($"Minimum Weight : {totalMinWeight:F1}");
        Debug.Log($"Maximum Weight : {totalMaxWeight:F1}");
        Debug.Log($"Average Weight : {averageWeight:F1}");

        Debug.Log("");

        Debug.Log("===== Suggested Rank =====");

        Debug.Log($"Rank S : {Mathf.RoundToInt(averageScore * 0.95f)}");
        Debug.Log($"Rank A : {Mathf.RoundToInt(averageScore * 0.80f)}");
        Debug.Log($"Rank B : {Mathf.RoundToInt(averageScore * 0.60f)}");

        Debug.Log("");

        Debug.Log("===== Validation =====");

        Debug.Log($"Warnings : {warningCount}");
        Debug.Log($"Errors : {errorCount}");

        if (errorCount == 0 && warningCount == 0)
        {
            Debug.Log("✅ LEVEL READY");
        }
        else if (errorCount == 0)
        {
            Debug.Log("⚠ LEVEL PLAYABLE (Review Recommended)");
        }
        else
        {
            Debug.LogError("❌ LEVEL HAS ERRORS");
        }

        Debug.Log("=======================================");
    }

    private void Warning(string message)
    {
        warningCount++;
        Debug.LogWarning("⚠ " + message);
    }

    private void Error(string message)
    {
        errorCount++;
        Debug.LogError("❌ " + message);
    }
}