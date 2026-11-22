
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

    #region PUBLIC PROPERTIES
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
    #endregion

    #region SPAWNED
    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        // Đặt giai đoạn ban đầu là Fixed Quest.
        if (Status == QuestStatus.NotStarted)
        {
            CurrentPhase = QuestPhase.FixedQuest;
        }

        Debug.Log(
            $"[Quest] Spawned Player: {Object.InputAuthority} | " +
            $"Waiting for Interact..."
        );
    }
    #endregion

    #region ACCEPT FIXED QUEST
    public void RequestAcceptFixedQuest()
    {
        Debug.Log(
            $"[Quest] RequestAcceptFixedQuest | " +
            $"HasInputAuthority: {Object.HasInputAuthority} | " +
            $"HasStateAuthority: {Object.HasStateAuthority}"
        );

        if (Object == null || !Object.IsValid)
        {
            Debug.LogError("[Quest] NetworkObject is invalid!");
            return;
        }

        if (HasStateAuthority)
        {
            Debug.Log("[Quest] Executing AcceptFixedQuest directly.");
            AcceptFixedQuest();
            return;
        }

        if (Object.HasInputAuthority)
        {
            Debug.Log("[Quest] Sending RPC_RequestAcceptFixedQuest...");
            RPC_RequestAcceptFixedQuest();
        }
        else
        {
            Debug.LogWarning(
                "[Quest] Cannot request quest: Player has no InputAuthority!"
            );
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAcceptFixedQuest()
    {
        Debug.Log("[Quest] RPC_RequestAcceptFixedQuest received by StateAuthority.");

        AcceptFixedQuest();
    }

    private void AcceptFixedQuest()
    {
        Debug.Log("[Quest] AcceptFixedQuest called.");

        if (!HasStateAuthority)
        {
            Debug.LogWarning("[Quest] Rejected: No StateAuthority.");
            return;
        }

        if (Database == null)
        {
            Debug.LogError("[Quest] Rejected: QuestDatabase is NULL!");
            return;
        }

        Debug.Log(
            $"[Quest] Current Status: {Status} | Current Phase: {CurrentPhase}"
        );

        if (Status != QuestStatus.NotStarted)
        {
            Debug.LogWarning(
                $"[Quest] Rejected: Status is {Status}, expected NotStarted."
            );
            return;
        }

        if (CurrentPhase != QuestPhase.FixedQuest)
        {
            Debug.LogWarning(
                $"[Quest] Rejected: Phase is {CurrentPhase}, expected FixedQuest."
            );
            return;
        }

        Debug.Log("[Quest] Conditions passed. Starting Fixed Quest...");

        StartFixedQuest();
    }
    #endregion

    #region REQUEST START OR CONTINUE QUEST
    // Dùng để tiếp tục Quest đang làm.
    // Không tự động cấp Quest mới.
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
        if (!HasStateAuthority || Database == null)
            return false;

        if (Status == QuestStatus.InProgress)
        {
            Debug.Log(
                $"[Quest] Continue: {CurrentQuestID}"
            );

            return true;
        }

        // Không tự nhận Quest khi chưa Interact.
        if (Status == QuestStatus.NotStarted)
        {
            Debug.Log(
                "[Quest] Player has not accepted a Quest yet."
            );

            return false;
        }

        // Quest Completed sẽ do RewardManager xử lý.
        return false;
    }
    #endregion

    #region START FIXED QUEST
    private void StartFixedQuest()
    {
        Debug.Log("[Quest] StartFixedQuest called.");

        if (Database == null)
        {
            Debug.LogError("[Quest] Database is NULL!");
            return;
        }

        QuestSO fixedQuest = Database.GetFixedQuest();

        if (fixedQuest == null)
        {
            Debug.LogError("[Quest] No Fixed Quest found in Database!");
            return;
        }

        Debug.Log(
            $"[Quest] Found Fixed Quest: {fixedQuest.questName} | " +
            $"ID: {fixedQuest.questID} | Category: {fixedQuest.questCategory}"
        );

        if (fixedQuest.questCategory != QuestCategory.Fixed)
        {
            Debug.LogError("[Quest] Quest category is not Fixed!");
            return;
        }

        // if (IsQuestCompleted(fixedQuest.questID))
        // {
        //     Debug.LogWarning(
        //         $"[Quest] Fixed Quest already completed: {fixedQuest.questID}"
        //     );
        //     return;
        // }

        bool success = StartNewQuest(fixedQuest);

        Debug.Log(
            success
                ? "[Quest] Fixed Quest started successfully!"
                : "[Quest] Failed to start Fixed Quest!"
        );
    }
    #endregion

    #region START NEW QUEST
    private bool StartNewQuest(QuestSO quest)
    {
        #region BASIC VALIDATION

        Debug.Log("[Quest] StartNewQuest validation started.");

        if (!HasStateAuthority)
        {
            Debug.LogError("[Quest] Cannot start quest: No StateAuthority!");
            return false;
        }

        if (quest == null)
        {
            Debug.LogError("[Quest] Quest is NULL!");
            return false;
        }

        if (string.IsNullOrEmpty(quest.questID))
        {
            Debug.LogError("[Quest] Quest ID is empty!");
            return false;
        }

        if (quest.questID.Length > 32)
        {
            Debug.LogError(
                $"[Quest] Quest ID exceeds 32 characters: {quest.questID}"
            );
            return false;
        }

        if (quest.objectives == null)
        {
            Debug.LogError("[Quest] Objectives list is NULL!");
            return false;
        }

        if (quest.objectives.Count == 0)
        {
            Debug.LogError("[Quest] Quest has no Objectives!");
            return false;
        }

        if (quest.objectives.Count > 10)
        {
            Debug.LogError(
                $"[Quest] Too many Objectives: {quest.objectives.Count}. Maximum: 10."
            );
            return false;
        }

        Debug.Log($"[Quest] Quest ID: {quest.questID}");
        Debug.Log($"[Quest] Objective Count: {quest.objectives.Count}");

        #endregion

        #region OBJECTIVE VALIDATION

        for (int i = 0; i < quest.objectives.Count; i++)
        {
            QuestObjective data = quest.objectives[i];

            if (data == null)
            {
                Debug.LogError($"[Quest] Objective {i} is NULL!");
                return false;
            }

            if (string.IsNullOrEmpty(data.objectiveID))
            {
                Debug.LogError($"[Quest] Objective {i} has empty ID!");
                return false;
            }

            if (data.objectiveID.Length > 32)
            {
                Debug.LogError(
                    $"[Quest] Objective ID exceeds 32 characters: {data.objectiveID}"
                );
                return false;
            }

            if (data.requiredAmount <= 0)
            {
                Debug.LogError(
                    $"[Quest] Invalid RequiredAmount: {data.objectiveID}"
                );
                return false;
            }

            switch (data.objectiveType)
            {
                case ObjectiveType.Collect:

                    if (data.targetItem == null)
                    {
                        Debug.LogError(
                            $"[Quest] Collect Objective has no TargetItem: {data.objectiveID}"
                        );
                        return false;
                    }

                    break;

                case ObjectiveType.Defeat:

                    if (string.IsNullOrEmpty(data.targetID))
                    {
                        Debug.LogError(
                            $"[Quest] Defeat Objective has empty TargetID: {data.objectiveID}"
                        );
                        return false;
                    }

                    if (data.targetID.Length > 32)
                    {
                        Debug.LogError(
                            $"[Quest] TargetID exceeds 32 characters: {data.targetID}"
                        );
                        return false;
                    }

                    break;

                default:

                    Debug.LogError(
                        $"[Quest] Unsupported ObjectiveType: {data.objectiveType}"
                    );

                    return false;
            }

            Debug.Log(
                $"[Quest] Objective {i} Validated | " +
                $"ID: {data.objectiveID} | " +
                $"Type: {data.objectiveType} | " +
                $"Required: {data.requiredAmount}"
            );
        }

        #endregion

        #region CLEAR OLD OBJECTIVES

        for (int i = 0; i < 10; i++)
        {
            Objectives.Set(i, default(NetworkObjective));
        }

        #endregion

        #region SET QUEST DATA

        _currentQuestID = quest.questID;
        _objectiveCount = quest.objectives.Count;

        #endregion

        #region INITIALIZE OBJECTIVES

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

            Debug.Log(
                $"[Quest] Network Objective Initialized | " +
                $"Index: {i} | " +
                $"ID: {objective.ObjectiveID} | " +
                $"Type: {objective.Type} | " +
                $"Required: {objective.RequiredAmount}"
            );
        }

        #endregion

        #region SET QUEST STATUS

        _status = (byte)QuestStatus.InProgress;

        Debug.Log(
            $"[Quest] New Quest Started: {quest.questName} | " +
            $"ID: {quest.questID} | " +
            $"Phase: {CurrentPhase} | " +
            $"Objectives: {_objectiveCount}"
        );

        #endregion

        return true;
    }
    #endregion

    #region TRY START NEXT QUEST AFTER REWARD
    public bool TryStartNextQuestAfterReward()
    {
        if (!HasStateAuthority)
            return false;

        if (Status != QuestStatus.Completed)
            return false;

        if (Database == null)
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

        // FIXED -> RANDOM
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

        // RANDOM -> RANDOM
        else if (CurrentPhase == QuestPhase.RandomQuest)
        {
            nextQuest = GetNextRandomQuest();
        }

        // NO MORE QUEST
        if (nextQuest == null)
        {
            CurrentPhase = QuestPhase.AllCompleted;

            Debug.Log(
                "[Quest] All Quests Completed!"
            );

            return false;
        }

        // Kiểm tra Quest tiếp theo phải là Random.
        if (nextQuest.questCategory != QuestCategory.Random)
        {
            Debug.LogError(
                $"[Quest] Invalid Random Quest: {nextQuest.questID}"
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
    #endregion

    #region GET NEXT RANDOM QUEST
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
    #endregion

    #region COMPLETED QUEST HISTORY
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
    #endregion

    #region COLLECT PROGRESS
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
    #endregion

    #region DEFEAT PROGRESS
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
    #endregion

    #region APPLY PROGRESS
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
    #endregion

    #region CHECK QUEST COMPLETED
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
    #endregion

    #region GET OBJECTIVE
    public NetworkObjective GetObjective(int index)
    {
        if (index < 0 || index >= _objectiveCount)
            return default;

        return Objectives[index];
    }
    #endregion

    #region GET COMPLETED QUEST ID
    public string GetCompletedQuestID(int index)
    {
        if (index < 0 || index >= _completedQuestCount)
            return string.Empty;

        return CompletedQuestIDs[index].ToString();
    }
    #endregion
}
