
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("Quest List")]
    [SerializeField] private List<QuestSO> quests = new();

    public IReadOnlyList<QuestSO> Quests => quests;

    public QuestSO GetQuest(string questID)
    {
        return quests.Find(
            quest => quest != null &&
                     quest.questID == questID
        );
    }

    public QuestSO GetRandomQuest()
    {
        List<QuestSO> validQuests = quests.FindAll(
            quest => quest != null &&
                     !string.IsNullOrEmpty(quest.questID) &&
                     quest.objectives != null &&
                     quest.objectives.Count > 0
        );

        if (validQuests.Count == 0)
            return null;

        int randomIndex = Random.Range(0, validQuests.Count);

        return validQuests[randomIndex];
    }

    public List<QuestSO> GetAllQuests()
    {
        return new List<QuestSO>(quests);
    }

    public bool ContainsQuest(string questID)
    {
        return GetQuest(questID) != null;
    }
}
