using UnityEngine;

[CreateAssetMenu(fileName = "GameDurationTimeData", menuName = "Game Data/Game Duration Time Data")]
public class GameDurationTimeData : ScriptableObject
{
    [Header("Time Setting")]
    [Tooltip("Play time.")]
    [Min(10)]
    public float levelTime = 300f;

    [Tooltip("Restart when time is up.")]
    public bool restartWhenTimeOut = true;
}