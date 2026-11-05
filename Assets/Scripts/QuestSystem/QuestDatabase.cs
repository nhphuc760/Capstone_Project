using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quests/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("Quest Collection")]
    [SerializeField]
    private List<QuestSO> quests = new List<QuestSO>();

    private Dictionary<string, QuestSO> questLookup;

    private void InitializeLookup()
    {
        questLookup = new Dictionary<string, QuestSO>();

        foreach (QuestSO quest in quests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questID))
                continue;

            if (questLookup.ContainsKey(quest.questID))
            {
                Debug.LogWarning(
                    $"[QuestDatabase] Duplicate Quest ID: {quest.questID}"
                );

                continue;
            }

            questLookup.Add(quest.questID, quest);
        }
    }

    public QuestSO GetQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return null;

        if (questLookup == null)
            InitializeLookup();

        questLookup.TryGetValue(questID, out QuestSO quest);

        return quest;
    }

    public QuestSO GetRandomQuest()
    {
        List<QuestSO> availableQuests = new List<QuestSO>();

        foreach (QuestSO quest in quests)
        {
            if (quest == null)
                continue;

            if (string.IsNullOrEmpty(quest.questID))
                continue;

            if (quest.objectives == null || quest.objectives.Count == 0)
                continue;

            if (quest.objectives.Count > 10)
                continue;

            bool valid = true;

            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective == null ||
                    string.IsNullOrEmpty(objective.objectiveID) ||
                    string.IsNullOrEmpty(objective.targetID) ||
                    objective.requiredAmount <= 0 ||
                    objective.objectiveID.Length > 32 ||
                    objective.targetID.Length > 32)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                availableQuests.Add(quest);
        }

        if (availableQuests.Count == 0)
        {
            Debug.LogWarning("[QuestDatabase] No valid quests available!");
            return null;
        }

        int randomIndex = Random.Range(0, availableQuests.Count);

        return availableQuests[randomIndex];
    }

    public IReadOnlyList<QuestSO> GetAllQuests()
    {
        return quests;
    }

    public bool ContainsQuest(string questID)
    {
        return GetQuest(questID) != null;
    }

    private void OnValidate()
    {
        questLookup = null;
    }
}