using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkQuestSys : NetworkBehaviour
{
    [Networked, Capacity(10)]
    private NetworkArray<NetworkQuest> Quests => default;

    [SerializeField] private QuestDatabase questDatabase;

    #region Quest Struct
    public struct NetworkQuest : INetworkStruct
    {
        public NetworkString<_32> questID;
        public NetworkBool active;
        public NetworkBool completed;

        [Networked, Capacity(10)]
        public NetworkArray<NetworkObjective> Objectives => default;
    }

    public struct NetworkObjective : INetworkStruct
    {
        public NetworkString<_32> objectiveID;
        public NetworkString<_32> targetID;
        public int currentAmount;
        public int requiredAmount;
        public NetworkBool completed;
    }
    #endregion

    #region Spawned
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Debug.Log("[NetworkQuestSys] Spawned | State Authority");
        }
        else
        {
            Debug.Log("[NetworkQuestSys] Spawned | Client");
        }
    }
    #endregion

    #region Quest
    public void StartQuest(string questID)
    {
        if (!HasStateAuthority)
            return;

        QuestSO quest = questDatabase.GetQuest(questID);

        if (quest == null)
        {
            Debug.LogWarning($"[NetworkQuestSys] Quest not found | ID={questID}");
            return;
        }

        for (int i = 0; i < Quests.Length; i++)
        {
            if (!string.IsNullOrEmpty(Quests[i].questID.ToString()))
                continue;

            NetworkQuest networkQuest = new NetworkQuest
            {
                questID = quest.questID,
                active = true,
                completed = false
            };

            for (int j = 0; j < quest.objectives.Count && j < 10; j++)
            {
                QuestObjective objective = quest.objectives[j];

                networkQuest.Objectives.Set(j, new NetworkObjective
                {
                    objectiveID = objective.objectiveID,
                    targetID = objective.targetID,
                    currentAmount = 0,
                    requiredAmount = objective.requiredAmount,
                    completed = false
                });
            }

            Quests.Set(i, networkQuest);

            Debug.Log($"[NetworkQuestSys] Quest Started | ID={quest.questID}");
            return;
        }

        Debug.LogWarning("[NetworkQuestSys] Quest capacity reached");
    }
    #endregion

    #region Objective Progress
    public void AddProgress(string questID, string objectiveID, int amount)
    {
        if (!HasStateAuthority)
            return;

        if (amount <= 0)
            return;

        for (int i = 0; i < Quests.Length; i++)
        {
            NetworkQuest quest = Quests[i];

            if (quest.questID.ToString() != questID)
                continue;

            if (!quest.active || quest.completed)
                return;

            for (int j = 0; j < quest.Objectives.Length; j++)
            {
                NetworkObjective objective = quest.Objectives[j];

                if (objective.objectiveID.ToString() != objectiveID)
                    continue;

                if (objective.completed)
                    return;

                objective.currentAmount += amount;

                if (objective.currentAmount >= objective.requiredAmount)
                {
                    objective.currentAmount = objective.requiredAmount;
                    objective.completed = true;
                }

                quest.Objectives.Set(j, objective);

                CheckQuestCompleted(ref quest);

                Quests.Set(i, quest);

                Debug.Log(
                    $"[NetworkQuestSys] Progress | Quest={questID} | " +
                    $"Objective={objectiveID} | " +
                    $"{objective.currentAmount}/{objective.requiredAmount}"
                );

                return;
            }
        }
    }
    #endregion

    #region Quest Completion
    private void CheckQuestCompleted(ref NetworkQuest quest)
    {
        for (int i = 0; i < quest.Objectives.Length; i++)
        {
            NetworkObjective objective = quest.Objectives[i];

            if (string.IsNullOrEmpty(objective.objectiveID.ToString()))
                continue;

            if (!objective.completed)
            {
                quest.completed = false;
                return;
            }
        }

        quest.completed = true;
        quest.active = false;

        Debug.Log(
            $"[NetworkQuestSys] Quest Completed | ID={quest.questID}"
        );
    }
    #endregion

    #region Get Quest
    public bool IsQuestActive(string questID)
    {
        for (int i = 0; i < Quests.Length; i++)
        {
            if (Quests[i].questID.ToString() == questID)
                return Quests[i].active;
        }

        return false;
    }

    public bool IsQuestCompleted(string questID)
    {
        for (int i = 0; i < Quests.Length; i++)
        {
            if (Quests[i].questID.ToString() == questID)
                return Quests[i].completed;
        }

        return false;
    }
    #endregion
}