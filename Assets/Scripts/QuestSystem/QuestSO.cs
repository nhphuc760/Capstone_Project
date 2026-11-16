
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/Quest Data")]
public class QuestSO : ScriptableObject
{
    [Header("Quest Information")]
    public string questID;
    public string questName;
    [TextArea(2, 5)]
    public string description;

    [Header("Objectives")]
    public List<QuestObjective> objectives = new();

    [Header("Random Valuable Rewards")]
    public ItemSO[] rewardItems;

    [Min(1)]
    public int rewardAmount = 1;

    private void OnValidate()
    {
        if (objectives == null)
            objectives = new List<QuestObjective>();

        if (rewardItems == null)
            rewardItems = new ItemSO[0];
    }
}
