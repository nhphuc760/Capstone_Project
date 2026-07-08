using UnityEngine;

public enum PointRank
{
    F,
    D,
    C,
    B,
    A,
    S,
    SS
}

[CreateAssetMenu(fileName = "PointData", menuName = "Scriptable Objects/Point Data")]
public class PointData : ScriptableObject
{
    [Header("Target")]
    [Tooltip("Point minimum")]
    public int passScore = 500;

    [Tooltip("Point maximum")]
    public int maxScore = 1000;

    [Header("Rank")]
    public int scoreD = 200;
    public int scoreC = 400;
    public int scoreB = 600;
    public int scoreA = 700;
    public int scoreS = 850;
    public int scoreSS = 1000;

    public PointRank GetRank(int score)
    {
        if (score >= scoreSS)
            return PointRank.SS;

        if (score >= scoreS)
            return PointRank.S;

        if (score >= scoreA)
            return PointRank.A;

        if (score >= scoreB)
            return PointRank.B;

        if (score >= scoreC)
            return PointRank.C;

        if (score >= scoreD)
            return PointRank.D;

        return PointRank.F;
    }

    public bool IsPass(int score)
    {
        return score >= passScore;
    }
}