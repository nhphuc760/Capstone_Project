
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("Quest List")]
    [SerializeField] private List<QuestSO> quests = new();

    public IReadOnlyList<QuestSO> Quests => quests;

    // =========================================
    // GET QUEST
    // =========================================

    public QuestSO GetQuest(string questID)
    {
        return quests.Find(quest =>
            quest != null &&
            quest.questID == questID
        );
    }

    // =========================================
    // GET FIXED QUEST
    // =========================================

    public QuestSO GetFixedQuest()
    {
        return quests.Find(quest =>
            IsValidQuest(quest) &&
            quest.questCategory == QuestCategory.Fixed
        );
    }

    // =========================================
    // GET RANDOM QUEST
    // =========================================

    public QuestSO GetRandomQuest(
        List<string> excludedQuestIDs)
    {
        List<QuestSO> validQuests = quests.FindAll(quest =>
            IsValidQuest(quest) &&
            quest.questCategory == QuestCategory.Random &&
            !excludedQuestIDs.Contains(quest.questID)
        );

        if (validQuests.Count == 0)
            return null;

        int randomIndex = Random.Range(
            0,
            validQuests.Count
        );

        return validQuests[randomIndex];
    }

    // =========================================
    // VALIDATE
    // =========================================

    private bool IsValidQuest(QuestSO quest)
    {
        return quest != null &&
               !string.IsNullOrEmpty(quest.questID) &&
               quest.questID.Length <= 32 &&
               quest.objectives != null &&
               quest.objectives.Count > 0 &&
               quest.objectives.Count <= 10;
    }

    // =========================================
    // OTHER METHODS
    // =========================================

    public List<QuestSO> GetAllQuests()
    {
        return new List<QuestSO>(quests);
    }

    public bool ContainsQuest(string questID)
    {
        return GetQuest(questID) != null;
    }
}
