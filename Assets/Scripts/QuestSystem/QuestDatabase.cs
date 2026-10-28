using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quests/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [SerializeField] private List<QuestSO> quests = new List<QuestSO>();
    private Dictionary<string, QuestSO> questLookup;

    private void InitializeLookup()
    {
        questLookup = new Dictionary<string, QuestSO>();

        foreach (var quest in quests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questID))
                continue;

            if (!questLookup.ContainsKey(quest.questID))
            {
                questLookup.Add(quest.questID, quest);
            }
            else
            {
                Debug.LogWarning($"[QuestDatabase] Phat hien trung lap questID: '{quest.questID}' tai asset '{quest.name}'");
            }
        }
    }

    #region Ham lay Quest theo questID (phuc vu NetworkQuestSys)
    public QuestSO GetQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return null;

        if (questLookup == null)
        {
            InitializeLookup();
        }

        if (questLookup.TryGetValue(questID, out QuestSO quest))
        {
            return quest;
        }

        return null;
    }
    #endregion

    private void OnValidate()
    {
        questLookup = null;
    }

    public bool ContainsQuest(string questID)
    {
        return GetQuest(questID) != null;
    }

    public IReadOnlyList<QuestSO> GetAllQuests()
    {
        return quests;
    }
}