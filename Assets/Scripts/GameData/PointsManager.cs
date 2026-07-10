using UnityEngine;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance;

    [Header("Current Level")]
    public LevelData currentLevel;

    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddScore(ObjectData objectData)
    {
        CurrentScore += objectData.objectPoints;

        Debug.Log(
            $"Collected {objectData.objectName}\n" +
            $" +{objectData.objectPoints} Score\n" +
            $"Total : {CurrentScore}"
        );
    }

    public void ResetScore()
    {
        CurrentScore = 0;
    }

    public PointRank GetRank()
    {
        return currentLevel.pointData.GetRank(CurrentScore);
    }

    public bool IsPass()
    {
        return currentLevel.pointData.IsPass(CurrentScore);
    }

    public void FinishLevel()
    {
        PointRank rank = GetRank();

        Debug.Log("===============");
        Debug.Log($"Score : {CurrentScore}");
        Debug.Log($"Rank : {rank}");
        Debug.Log($"Pass : {IsPass()}");
        Debug.Log("===============");
    }
}
