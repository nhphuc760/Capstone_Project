using UnityEngine;
using System;
using System.Collections.Generic;

public enum ObjectiveType 
{ 
    Collect, 
    Defeat 
}

[Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string targetID;
    public string description;
    public ObjectiveType type;
    public int requiredAmount;
    public int currentAmount;

    public bool IsCompleted => currentAmount >= requiredAmount;
}

[CreateAssetMenu(menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;
    public List<QuestObjective> objectives;

    #region Auto Generate QuestID
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(questID))
        {
            questID = Guid.NewGuid().ToString("N");
        }
    }
    #endregion
}