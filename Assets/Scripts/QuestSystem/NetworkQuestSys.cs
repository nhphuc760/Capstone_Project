using Fusion;
using UnityEngine;

public class NetworkQuestSys : NetworkBehaviour
{
    [Header("Quest Data")]
    [SerializeField] private QuestDatabase questDatabase;

    private const int MAX_OBJECTIVES = 10;

    #region Networked Quest Data
    [Networked]
    private NetworkString<_32> _currentQuestID { get; set; }

    [Networked]
    private NetworkBool _hasAssignedQuest { get; set; }

    [Networked]
    private NetworkBool _questActive { get; set; }

    [Networked]
    private NetworkBool _questCompleted { get; set; }

    [Networked]
    private int _objectiveCount { get; set; }

    [Networked, Capacity(MAX_OBJECTIVES)]
    private NetworkArray<NetworkObjective> Objectives => default;
    #endregion

    #region Network Objective Struct
    public struct NetworkObjective : INetworkStruct
    {
        public NetworkString<_32> objectiveID;
        public NetworkString<_32> targetID;

        public byte objectiveType;

        public int currentAmount;
        public int requiredAmount;

        public NetworkBool completed;
    }
    #endregion

    #region Properties
    public QuestDatabase Database => questDatabase;

    public string CurrentQuestID => _currentQuestID.ToString();

    public bool HasAssignedQuest => _hasAssignedQuest;

    public bool IsActive => _questActive;

    public bool IsCompleted => _questCompleted;

    public int ObjectiveCount => _objectiveCount;
    #endregion

    #region Spawned
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Debug.Log(
                $"[NetworkQuestSys] Spawned | Player={Object.InputAuthority}"
            );

            // Only assign a quest once.
            if (!_hasAssignedQuest)
            {
                AssignRandomQuest();
            }
        }
        else
        {
            Debug.Log(
                $"[NetworkQuestSys] Client Spawned | Player={Object.InputAuthority}"
            );
        }
    }
    #endregion

    #region Random Quest
    private void AssignRandomQuest()
    {
        if (!HasStateAuthority)
            return;

        if (_hasAssignedQuest)
            return;

        if (questDatabase == null)
        {
            Debug.LogError(
                "[NetworkQuestSys] QuestDatabase is missing!"
            );

            return;
        }

        QuestSO randomQuest = questDatabase.GetRandomQuest();

        if (randomQuest == null)
            return;

        StartQuest(randomQuest);

        Debug.Log(
            $"[NetworkQuestSys] Random Quest Assigned | " +
            $"Player={Object.InputAuthority} | " +
            $"Quest={randomQuest.questName} | " +
            $"ID={randomQuest.questID}"
        );
    }
    #endregion

    #region Start Quest
    private void StartQuest(QuestSO quest)
    {
        if (!HasStateAuthority)
            return;

        if (quest == null)
            return;

        if (_hasAssignedQuest)
            return;

        if (quest.objectives == null ||
            quest.objectives.Count == 0 ||
            quest.objectives.Count > MAX_OBJECTIVES)
        {
            Debug.LogWarning(
                "[NetworkQuestSys] Invalid Quest Objectives!"
            );

            return;
        }

        _currentQuestID = quest.questID;

        _objectiveCount = quest.objectives.Count;

        for (int i = 0; i < _objectiveCount; i++)
        {
            QuestObjective objective = quest.objectives[i];

            Objectives.Set(i, new NetworkObjective
            {
                objectiveID = objective.objectiveID,
                targetID = objective.targetID,

                objectiveType = (byte)objective.type,

                currentAmount = 0,
                requiredAmount = objective.requiredAmount,

                completed = false
            });
        }

        _questActive = true;
        _questCompleted = false;

        // Set this last, after successful initialization.
        _hasAssignedQuest = true;

        Debug.Log(
            $"[NetworkQuestSys] Quest Started | " +
            $"ID={quest.questID} | " +
            $"Objectives={_objectiveCount}"
        );
    }
    #endregion

    #region Objective Progress
    // Use this when updating by ObjectiveType and TargetID.
    public void AddProgress(
        ObjectiveType type,
        string targetID,
        int amount)
    {
        if (!HasStateAuthority)
            return;

        if (!_questActive || _questCompleted)
            return;

        if (amount <= 0 || string.IsNullOrEmpty(targetID))
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            NetworkObjective objective = Objectives[i];

            if (objective.completed)
                continue;

            if (objective.objectiveType != (byte)type)
                continue;

            if (objective.targetID.ToString() != targetID)
                continue;

            ApplyProgress(i, amount);
        }
    }

    // Use this when updating a specific Objective ID.
    public void AddProgress(
        string objectiveID,
        int amount)
    {
        if (!HasStateAuthority)
            return;

        if (!_questActive || _questCompleted)
            return;

        if (amount <= 0 || string.IsNullOrEmpty(objectiveID))
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            NetworkObjective objective = Objectives[i];

            if (objective.objectiveID.ToString() != objectiveID)
                continue;

            ApplyProgress(i, amount);
            return;
        }
    }

    private void ApplyProgress(int index, int amount)
    {
        NetworkObjective objective = Objectives[index];

        if (objective.completed)
            return;

        objective.currentAmount = Mathf.Min(
            objective.currentAmount + amount,
            objective.requiredAmount
        );

        if (objective.currentAmount >= objective.requiredAmount)
        {
            objective.completed = true;
        }

        Objectives.Set(index, objective);

        CheckQuestCompleted();

        Debug.Log(
            $"[NetworkQuestSys] Progress | " +
            $"Quest={CurrentQuestID} | " +
            $"Objective={objective.objectiveID} | " +
            $"{objective.currentAmount}/{objective.requiredAmount}"
        );
    }
    #endregion

    #region Quest Completion
    private void CheckQuestCompleted()
    {
        if (!HasStateAuthority)
            return;

        for (int i = 0; i < _objectiveCount; i++)
        {
            if (!Objectives[i].completed)
                return;
        }

        _questCompleted = true;
        _questActive = false;

        Debug.Log(
            $"[NetworkQuestSys] Quest Completed | " +
            $"Player={Object.InputAuthority} | " +
            $"Quest={CurrentQuestID}"
        );
    }
    #endregion

    #region Get Objective
    public bool TryGetObjective(
        int index,
        out NetworkObjective objective)
    {
        objective = default;

        if (index < 0 || index >= _objectiveCount)
            return false;

        objective = Objectives[index];

        return true;
    }
    #endregion

    #region Quest Status
    public bool IsQuestActive(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        return CurrentQuestID == questID && _questActive;
    }

    public bool IsQuestCompleted(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        return CurrentQuestID == questID && _questCompleted;
    }
    #endregion
}