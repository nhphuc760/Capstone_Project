// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;

// public class LevelBalanceWindow : EditorWindow
// {
//     private LevelData levelData;

//     private int warningCount;
//     private int errorCount;

//     private Vector2 scrollPosition;

//     private enum LogType { Info, Warning, Error }

//     private class AnalyzerLog
//     {
//         public string message;
//         public LogType type;
//         public AnalyzerLog(string m, LogType t){ message=m; type=t; }
//     }

//     private readonly List<AnalyzerLog> logs = new();

//     [MenuItem("Tools/Test Tools/Level Balance Analyzer")]
//     public static void OpenWindow()
//     {
//         GetWindow<LevelBalanceWindow>("Level Balance");
//     }

//     private void OnGUI()
//     {
//         GUILayout.Space(10);
//         GUILayout.Label("Level Balance Analyzer", EditorStyles.boldLabel);

//         levelData = (LevelData)EditorGUILayout.ObjectField(
//             "Level Data", levelData, typeof(LevelData), false);

//         GUILayout.Space(10);

//         if (GUILayout.Button("Analyze Level", GUILayout.Height(30)))
//         {
//             AnalyzeLevel();
//         }

//         GUILayout.Space(10);
//         GUILayout.Label("Analysis Result", EditorStyles.boldLabel);

//         scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//         foreach (var log in logs)
//         {
//             MessageType type = MessageType.Info;
//             if (log.type == LogType.Warning) type = MessageType.Warning;
//             else if (log.type == LogType.Error) type = MessageType.Error;

//             EditorGUILayout.HelpBox(log.message, type);
//         }

//         EditorGUILayout.EndScrollView();
//     }

//     private void AnalyzeLevel()
//     {
//         logs.Clear();
//         warningCount = 0;
//         errorCount = 0;

//         if (levelData == null)
//         {
//             Debug.LogError("❌ Please assign a LevelData.");
//             return;
//         }

//         if (levelData.areas == null || levelData.areas.Count == 0)
//         {
//             Debug.LogError("❌ Level has no Area.");
//             return;
//         }

//         int totalMinScore = 0;
//         int totalMaxScore = 0;

//         float totalMinWeight = 0;
//         float totalMaxWeight = 0;

//         int totalMinObjects = 0;
//         int totalMaxObjects = 0;

//         Log($"LEVEL : {levelData.levelName}");

//         foreach (AreaData area in levelData.areas)
//         {
//             if (area.spawnDatas == null || area.spawnDatas.Count == 0)
//             {
//                 Warning($"Area [{area.areaType}] has no SpawnData.");
//                 continue;
//             }

//             Log($"AREA : {area.areaType}");

//             foreach (SpawnData spawn in area.spawnDatas)
//             {
//                 //----------------------------------------
//                 // Validation
//                 //----------------------------------------

//                 if (spawn.objectData == null)
//                 {
//                     Error($"Area [{area.areaType}] contains SpawnData without ObjectData.");
//                     continue;
//                 }

//                 if (spawn.minAmount > spawn.maxAmount)
//                 {
//                     Error($"{spawn.objectData.objectName} : MinAmount > MaxAmount.");
//                 }

//                 if (spawn.objectData.objectPoints <= 0)
//                 {
//                     Warning($"{spawn.objectData.objectName} has 0 Point.");
//                 }

//                 if (spawn.objectData.weight <= 0)
//                 {
//                     Warning($"{spawn.objectData.objectName} has 0 Weight.");
//                 }

//                 if (spawn.spawnChance <= 0)
//                 {
//                     Warning($"{spawn.objectData.objectName} has SpawnChance = 0%.");
//                 }

//                 //----------------------------------------
//                 // Calculate
//                 //----------------------------------------

//                 int minScore =
//                     spawn.minAmount *
//                     spawn.objectData.objectPoints;

//                 int maxScore =
//                     spawn.maxAmount *
//                     spawn.objectData.objectPoints;

//                 float minWeight =
//                     spawn.minAmount *
//                     spawn.objectData.weight;

//                 float maxWeight =
//                     spawn.maxAmount *
//                     spawn.objectData.weight;

//                 totalMinScore += minScore;
//                 totalMaxScore += maxScore;

//                 totalMinWeight += minWeight;
//                 totalMaxWeight += maxWeight;

//                 totalMinObjects += spawn.minAmount;
//                 totalMaxObjects += spawn.maxAmount;

//                 Log(
//                     $"Object : {spawn.objectData.objectName}\n" +
//                     $"Spawn : {spawn.minAmount} - {spawn.maxAmount}\n" +
//                     $"Point : {spawn.objectData.objectPoints}\n" +
//                     $"Weight : {spawn.objectData.weight}\n" +
//                     $"Score : {minScore} - {maxScore}\n");
//             }

//             int averageScore = (totalMinScore + totalMaxScore) / 2;

//             float averageWeight =
//                 (totalMinWeight + totalMaxWeight) / 2f;

//             int averageObjects =
//                 (totalMinObjects + totalMaxObjects) / 2;

//             //----------------------------------------
//             // Time Validation
//             //----------------------------------------

//             if (levelData.timeData != null)
//             {
//                 float estimatedTime = averageObjects * 8f;

//                 Log($"Estimated Play Time : {estimatedTime:F1} seconds");

//                 if (estimatedTime > levelData.timeData.levelTime)
//                 {
//                     Warning(
//                         $"Estimated play time ({estimatedTime:F1}s) exceeds Level Time ({levelData.timeData.levelTime:F1}s).");
//                 }
//             }
//             else
//             {
//                 Warning("TimeData is missing.");
//             }

//             //----------------------------------------
//             // PointData Validation
//             //----------------------------------------

//             if (levelData.pointData != null)
//             {
//                 if (levelData.pointData.scoreB >= levelData.pointData.scoreA)
//                     Error("Rank B must be lower than Rank A.");

//                 if (levelData.pointData.scoreA >= levelData.pointData.scoreS)
//                     Error("Rank A must be lower than Rank S.");

//                 if (levelData.pointData.scoreS >= levelData.pointData.scoreSS)
//                     Error("Rank S must be lower than Rank SS.");

//                 if (totalMaxScore < levelData.pointData.scoreB)
//                 {
//                     Error("Maximum obtainable score is lower than Rank B.");
//                 }
//             }
//             else
//             {
//                 Warning("PointData is missing.");
//             }

//             //----------------------------------------
//             // Result
//             //----------------------------------------

//             Log("================ RESULT ================");

//             Log($"Minimum Objects : {totalMinObjects}");
//             Log($"Maximum Objects : {totalMaxObjects}");
//             Log($"Average Objects : {averageObjects}");

//             Log("================= SCORE ================");

//             Log($"Minimum Score : {totalMinScore}");
//             Log($"Maximum Score : {totalMaxScore}");
//             Log($"Average Score : {averageScore}");

//             Log("================= WEIGHT ================");

//             Log($"Minimum Weight : {totalMinWeight:F1}");
//             Log($"Maximum Weight : {totalMaxWeight:F1}");
//             Log($"Average Weight : {averageWeight:F1}");

//             Log("================= RANKS ================");

//             Log("===== Suggested Rank =====");

//             Log($"Rank S : {Mathf.RoundToInt(averageScore * 0.95f)}");
//             Log($"Rank A : {Mathf.RoundToInt(averageScore * 0.80f)}");
//             Log($"Rank B : {Mathf.RoundToInt(averageScore * 0.60f)}");

//             Log("======== Rank Validation ======");

//             Log("===== Validation =====");

//             Log($"Warnings : {warningCount}");
//             Log($"Errors : {errorCount}");

//             if (errorCount == 0 && warningCount == 0)
//             {
//                 Log("✅ LEVEL READY");
//             }
//             else if (errorCount == 0)
//             {
//                 Log("⚠ LEVEL PLAYABLE (Review Recommended)");
//             }
//             else
//             {
//                 Log("❌ LEVEL HAS ERRORS");
//             }
//         }
//     }

//     private void Log(string message)
//     {
//         logs.Add(new AnalyzerLog(message, LogType.Info));
//     }

//     private void Warning(string message)
//     {
//         warningCount++;
//         logs.Add(new AnalyzerLog(message, LogType.Warning));
//     }

//     private void Error(string message)
//     {
//         errorCount++;
//         logs.Add(new AnalyzerLog(message, LogType.Error));
//     }
// }
