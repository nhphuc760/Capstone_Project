using System;
using System.Collections.Generic;

[Serializable]
public class QuestProgress
{
    public QuestSO quest;
    public List<QuestObjective> objectives;

    public QuestProgress(QuestSO quest)
    {
        #region Save the quest and initialize objectives
        this.quest = quest;
        objectives = new List<QuestObjective>();

        foreach (var obj in quest.objectives)
        {
            objectives.Add(new QuestObjective
            {
                objectiveID = obj.objectiveID,
                targetID = obj.targetID,
                description = obj.description,
                type = obj.type,
                requiredAmount = obj.requiredAmount,
                currentAmount = 0
            });
        }
        #endregion
    }

    public bool IsCompleted() => objectives.TrueForAll(o => o.IsCompleted);
    public string QuestID => quest.questID;
}