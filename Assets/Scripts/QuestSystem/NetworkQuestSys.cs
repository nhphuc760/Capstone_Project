
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// =========================================
// QUEST PHASE
// =========================================

public enum QuestPhase : byte
{
    FixedQuest,
    RandomQuest,
    AllCompleted
}

// =========================================
// NETWORK OBJECTIVE
// =========================================

public struct NetworkObjective : INetworkStruct
{
    public NetworkString<_32> ObjectiveID;
    public NetworkString<_32> TargetID;

    public int TargetItemID;

    public byte Type;

    public int CurrentAmount;
    public int RequiredAmount;

    public NetworkBool Completed;
}

// =========================================
// NETWORK QUEST SYSTEM
// =========================================

public class NetworkQuestSys : NetworkBehaviour
{
    [Header("Quest Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Network Quest Data")]
    [Networked]
    private NetworkString<_32> _currentQuestID { get; set; }

    [Networked]
    private byte _status { get; set; }

    [Networked]
    private int _objectiveCount { get; set; }

    [Header("Quest Phase")]
    [Networked]
    public QuestPhase CurrentPhase { get; set; }

    [Header("Completed Quest History")]
    [Networked]
    private int _completedQuestCount { get; set; }

    [Networked, Capacity(64)]
    private NetworkArray<NetworkString<_32>> CompletedQuestIDs => default;

    [Networked, Capacity(10)]
    public NetworkArray<NetworkObjective> Objectives => default;

    // =========================================
    // PUBLIC PROPERTIES
    // =========================================

    public QuestDatabase Database => questDatabase;

    public string CurrentQuestID => _currentQuestID.ToString();

    public QuestStatus Status => (QuestStatus)_status;

    public int ObjectiveCount => _objectiveCount;

    public int CompletedQuestCount => _completedQuestCount;

    public bool IsCompleted =>
        Status == QuestStatus.Completed;

    public bool IsActive =>
        Status == QuestStatus.InProgress;

    public bool HasStarted =>
        Status != QuestStatus.NotStarted;

    public bool IsAllCompleted =>
        CurrentPhase == QuestPhase.AllCompleted;

    // =========================================
    // SPAWNED
    // =========================================

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        Debug.Log(
            $"[Quest] Spawned Player: {Object.InputAuthority}"
        );

        // Tự động giao Fixed Quest khi Player vào game.
        if (Status == QuestStatus.NotStarted)
        {
            StartFixedQuest();
        }
    }

    // =========================================
    // PLAY BUTTON
    // =========================================

    public void RequestStartOrContinueQuest()
    {
        if (HasStateAuthority)
        {
            StartOrContinueQuest();
            return;
        }

        if (Object != null && Object.HasInputAuthority)
        {
            RPC_RequestStartOrContinueQuest();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestStartOrContinueQuest()
    {
        StartOrContinueQuest();
    }

    private bool StartOrContinueQuest()
    {
        if (!HasStateAuthority || questDatabase == null)
            return false;

        if (Status == QuestStatus.InProgress)
        {
            Debug.Log(
                $"[Quest] Continue: {CurrentQuestID}"
            );

            return true;
        }

        if (Status == QuestStatus.NotStarted)
        {
            return StartFixedQuest();
        }

        // Quest Completed sẽ do RewardManager xử lý.
        return false;
    }

    // =========================================
    // START FIXED QUEST
    // =========================================

    private bool StartFixedQuest()
    {
        if (!HasStateAuthority || questDatabase == null)
            return false;

        QuestSO fixedQuest = questDatabase.GetFixedQuest();

        if (fixedQuest == null)
        {
            Debug.LogWarning(
                "[Quest] No valid Fixed Quest found."
            );

            return false;
        }

        if (HasCompletedQuest(fixedQuest.questID))
        {
            Debug.LogWarning(
                "[Quest] Fixed Quest was already completed."
            );

            return false;
        }

        if (!StartNewQuest(fixedQuest))
            return false;

        CurrentPhase = QuestPhase.FixedQuest;

        Debug.Log(
            $"[Quest] Fixed Quest Started: {fixedQuest.questName}"
        );

        return true;
    }

    // =========================================
    // START NEW QUEST
    // =========================================

    private bool StartNewQuest(QuestSO quest)
    {
        if (!HasStateAuthority || quest == null)
            return false;

        if (string.IsNullOrEmpty(quest.questID) ||
            quest.questID.Length > 32)
            return false;

        if (quest.objectives == null ||
            quest.objectives.Count == 0 ||
            quest.objectives.Count > 10)
            return false;

        // Validate Objective trước khi ghi Networked Data.
        foreach (QuestObjective data in quest.objectives)
        {
            if (data == null ||
                string.IsNullOrEmpty(data.objectiveID) ||
                data.objectiveID.Length > 32)
                return false;

            if (data.requiredAmount <= 0)
                return false;

            if (data.objectiveType == ObjectiveType.Collect)
            {
                if (data.targetItem == null)
                    return false;
            }
            else if (data.objectiveType == ObjectiveType.Defeat)
            {
                if (string.IsNullOrEmpty(data.targetID) ||
                    data.targetID.Length > 32)
                    return false;
            }
            else
            {
                return false;
            }
        }

        // Xóa Objective cũ.
        for (int i = 0; i < 10; i++)
        {
            Objectives.Set(i, default);
        }

        _currentQuestID = quest.questID;
        _objectiveCount = quest.objectives.Count;

        // Khởi tạo Objective mới.
        for (int i = 0; i < _objectiveCount; i++)
        {
            QuestObjective data = quest.objectives[i];

            NetworkObjective objective = new NetworkObjective
            {
                ObjectiveID = data.objectiveID,

                TargetID = data.objectiveType == ObjectiveType.Defeat
                    ? data.targetID
                    : string.Empty,

                TargetItemID = data.objectiveType == ObjectiveType.Collect
                    ? data.targetItem.ItemId
                    : 0,

                Type = (byte)data.objectiveType,

                CurrentAmount = 0,

                RequiredAmount = data.requiredAmount,

                Completed = false
            };

            Objectives.Set(i, objective);
        }

        _status = (byte)QuestStatus.InProgress;

        Debug.Log(
            $"[Quest] New Quest Started: {quest.questName} | " +
            $"ID: {quest.questID} | Phase: {CurrentPhase}"
        );

        return true;
    }

    // =========================================
    // TRY START NEXT QUEST AFTER REWARD
    // =========================================

    public bool TryStartNextQuestAfterReward()
    {
        if (!HasStateAuthority)
            return false;

        if (Status != QuestStatus.Completed)
            return false;

        if (questDatabase == null)
            return false;

        if (CurrentPhase == QuestPhase.AllCompleted)
            return false;

        // Lưu Quest vừa hoàn thành.
        if (!AddCompletedQuest(CurrentQuestID))
        {
            Debug.LogError(
                "[Quest] Cannot save completed Quest."
            );

            return false;
        }

        QuestSO nextQuest = null;

        // =====================================
        // FIXED -> RANDOM
        // =====================================

        if (CurrentPhase == QuestPhase.FixedQuest)
        {
            Debug.Log(
                "[Quest] Fixed Quest Completed. " +
                "Unlocking Random Quests..."
            );

            nextQuest = GetNextRandomQuest();

            if (nextQuest != null)
            {
                CurrentPhase = QuestPhase.RandomQuest;
            }
        }

        // =====================================
        // RANDOM -> RANDOM
        // =====================================

        else if (CurrentPhase == QuestPhase.RandomQuest)
        {
            nextQuest = GetNextRandomQuest();
        }

        // =====================================
        // NO MORE QUEST
        // =====================================

        if (nextQuest == null)
        {
            CurrentPhase = QuestPhase.AllCompleted;

            Debug.Log(
                "[Quest] All Quests Completed!"
            );

            return false;
        }

        // Bắt đầu Quest tiếp theo.
        if (!StartNewQuest(nextQuest))
        {
            Debug.LogError(
                $"[Quest] Failed to start Quest: {nextQuest.questID}"
            );

            return false;
        }

        Debug.Log(
            $"[Quest] Next Quest Started: {nextQuest.questName}"
        );

        return true;
    }

    // =========================================
    // GET NEXT RANDOM QUEST
    // =========================================

    private QuestSO GetNextRandomQuest()
    {
        if (!HasStateAuthority || questDatabase == null)
            return null;

        List<string> completedIDs = GetCompletedQuestList();

        QuestSO quest = questDatabase.GetRandomQuest(
            completedIDs
        );

        if (quest == null)
        {
            Debug.Log(
                "[Quest] No more available Random Quests."
            );
        }

        return quest;
    }

    // =========================================
    // COMPLETED QUEST HISTORY
    // =========================================

    private bool HasCompletedQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        for (int i = 0; i < _completedQuestCount; i++)
        {
            if (CompletedQuestIDs[i].ToString() == questID)
                return true;
        }

        return false;
    }

    private bool AddCompletedQuest(string questID)
    {
        if (!HasStateAuthority ||
            string.IsNullOrEmpty(questID))
            return false;

        if (HasCompletedQuest(questID))
            return true;

        if (_completedQuestCount >= 64)
        {
            Debug.LogWarning(
                "[Quest] Completed Quest History is full."
            );

            return false;
        }

        CompletedQuestIDs.Set(
            _completedQuestCount,
            questID
        );

        _completedQuestCount++;

        Debug.Log(
            $"[Quest] Added Completed Quest: {questID}"
        );

        return true;
    }

    private List<string> GetCompletedQuestList()
    {
        List<string> result = new List<string>();

        for (int i = 0; i < _completedQuestCount; i++)
        {
            result.Add(
                CompletedQuestIDs[i].ToString()
            );
        }

        return result;
    }

    // =========================================
    // COLLECT PROGRESS
    // =========================================

    public void AddCollectProgress(int itemID, int amount = 1)
    {
        if (!HasStateAuthority)
            return;

        if (Status != QuestStatus.InProgress || amount <= 0)
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            NetworkObjective objective = Objectives[i];

            if (objective.Completed)
                continue;

            if (objective.Type != (byte)ObjectiveType.Collect)
                continue;

            if (objective.TargetItemID != itemID)
                continue;

            ApplyProgress(i, amount);
        }
    }

    // =========================================
    // DEFEAT PROGRESS
    // =========================================

    public void AddDefeatProgress(string targetID, int amount = 1)
    {
        if (!HasStateAuthority)
            return;

        if (Status != QuestStatus.InProgress || amount <= 0)
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            NetworkObjective objective = Objectives[i];

            if (objective.Completed)
                continue;

            if (objective.Type != (byte)ObjectiveType.Defeat)
                continue;

            if (objective.TargetID.ToString() != targetID)
                continue;

            ApplyProgress(i, amount);
        }
    }

    // =========================================
    // APPLY PROGRESS
    // =========================================

    private void ApplyProgress(int index, int amount)
    {
        NetworkObjective objective = Objectives[index];

        objective.CurrentAmount = Mathf.Min(
            objective.CurrentAmount + amount,
            objective.RequiredAmount
        );

        objective.Completed =
            objective.CurrentAmount >= objective.RequiredAmount;

        Objectives.Set(index, objective);

        Debug.Log(
            $"[Quest] {objective.ObjectiveID}: " +
            $"{objective.CurrentAmount}/{objective.RequiredAmount}"
        );

        CheckQuestCompleted();
    }

    // =========================================
    // CHECK QUEST COMPLETED
    // =========================================

    private void CheckQuestCompleted()
    {
        if (!HasStateAuthority ||
            Status != QuestStatus.InProgress)
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            if (!Objectives[i].Completed)
                return;
        }

        _status = (byte)QuestStatus.Completed;

        Debug.Log(
            $"[Quest] Quest Completed: {CurrentQuestID}"
        );
    }

    // =========================================
    // GET OBJECTIVE
    // =========================================

    public NetworkObjective GetObjective(int index)
    {
        if (index < 0 || index >= _objectiveCount)
            return default;

        return Objectives[index];
    }

    // =========================================
    // GET COMPLETED QUEST ID
    // =========================================

    public string GetCompletedQuestID(int index)
    {
        if (index < 0 || index >= _completedQuestCount)
            return string.Empty;

        return CompletedQuestIDs[index].ToString();
    }
}
