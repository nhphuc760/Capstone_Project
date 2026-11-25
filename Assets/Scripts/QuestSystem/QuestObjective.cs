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

// Phân loại đối tượng cần thu thập
public enum CollectTargetType : byte
{
    Item,
    Resource
}

[Serializable]
public class QuestObjective
{
    #region Objective Information

    [Header("Objective Information")]
    public string objectiveID;
    public string description;

    [Header("Objective Type")]
    public ObjectiveType objectiveType;

    #endregion

    #region Collect Settings

    [Header("Collect Target")]
    public CollectTargetType collectTargetType;

    [Header("Item Target")]
    public ItemSO targetItem;

    [Header("Resource Target")]
    public ResourceType targetResource;

    #endregion

    #region Defeat Settings

    [Header("Defeat Target")]
    public string targetID;

    #endregion

    #region Requirement

    [Header("Requirement")]
    [Min(1)]
    public int requiredAmount = 1;

    #endregion
}