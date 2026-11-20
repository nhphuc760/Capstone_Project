
using System;
using UnityEngine;

public enum ObjectiveType
{
    Collect,
    Defeat
}

public enum QuestStatus : byte
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

[Serializable]
public class QuestObjective
{
    [Header("Objective Information")]
    public string objectiveID;
    public string description;

    [Header("Objective Type")]
    public ObjectiveType objectiveType;

    [Header("Collect Target")]
    public ItemSO targetItem;

    [Header("Defeat Target")]
    public string targetID; // dùng để lưu trữ ID của kẻ thù hoặc đối tượng cần đánh bại

    [Header("Requirement")]
    [Min(1)]
    public int requiredAmount = 1;
}
