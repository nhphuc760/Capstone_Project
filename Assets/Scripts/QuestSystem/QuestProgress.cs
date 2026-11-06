using System;
using System.Collections.Generic;

[Serializable]
public class QuestProgress
{
    public QuestSO quest;

    public List<QuestObjective> objectives =
        new List<QuestObjective>();

    public bool IsCompleted()
    {
        return objectives.Count > 0 &&
               objectives.TrueForAll(o => o.IsCompleted);
    }

    public string QuestID => quest != null ? quest.questID : string.Empty;

    public string QuestName => quest != null ? quest.questName : string.Empty;

    // Read Networked Data and update local progress.
    public bool RefreshFromNetwork(NetworkQuestSys source)
    {
        if (source == null || source.Database == null)
            return false;

        if (!source.HasAssignedQuest)
            return false;

        QuestSO sourceQuest = source.Database.GetQuest(
            source.CurrentQuestID
        );

        if (sourceQuest == null)
            return false;

        quest = sourceQuest;

        objectives.Clear();

        for (int i = 0; i < source.ObjectiveCount; i++)
        {
            if (!source.TryGetObjective(
                i,
                out NetworkQuestSys.NetworkObjective networkObjective))
            {
                continue;
            }

            objectives.Add(new QuestObjective
            {
                objectiveID = networkObjective.objectiveID.ToString(),

                targetID = networkObjective.targetID.ToString(),

                description = GetDescription(
                    sourceQuest,
                    networkObjective.objectiveID.ToString()
                ),

                type = (ObjectiveType)networkObjective.objectiveType,

                currentAmount = networkObjective.currentAmount,

                requiredAmount = networkObjective.requiredAmount
            });
        }

        return true;
    }

    private string GetDescription(
        QuestSO sourceQuest,
        string objectiveID)
    {
        foreach (QuestObjective objective in sourceQuest.objectives)
        {
            if (objective.objectiveID == objectiveID)
                return objective.description;
        }

        return string.Empty;
    }
}