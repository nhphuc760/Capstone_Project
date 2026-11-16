
using Fusion;
using UnityEngine;

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

public class NetworkQuestSys : NetworkBehaviour
{
    [Header("Quest Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Network Quest Data")]
    [Networked] private NetworkString<_32> _currentQuestID { get; set; }

    [Networked] private byte _status { get; set; }

    [Networked] private int _objectiveCount { get; set; }

    [Networked, Capacity(10)]
    public NetworkArray<NetworkObjective> Objectives => default;

    // =========================================
    // PUBLIC PROPERTIES
    // =========================================

    public QuestDatabase Database => questDatabase;

    public string CurrentQuestID => _currentQuestID.ToString();

    public QuestStatus Status => (QuestStatus)_status;

    public int ObjectiveCount => _objectiveCount;

    public bool IsCompleted => Status == QuestStatus.Completed;

    public bool IsActive => Status == QuestStatus.InProgress;

    public bool HasStarted => Status != QuestStatus.NotStarted;

    // =========================================
    // SPAWNED
    // =========================================

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Debug.Log(
                $"[Quest] Spawned Player: {Object.InputAuthority}"
            );
        }
    }

    // =========================================
    // PLAY BUTTON
    // =========================================

    // Gọi hàm này khi Player bấm Play.
    // Nếu gọi từ Client, RPC sẽ chuyển yêu cầu tới StateAuthority.

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
        if (!HasStateAuthority)
            return false;

        if (questDatabase == null)
            return false;

        // Đang làm Quest: giữ nguyên Quest hiện tại.
        if (Status == QuestStatus.InProgress)
        {
            Debug.Log(
                $"[Quest] Continue: {CurrentQuestID}"
            );

            return true;
        }

        // Chưa có Quest: nhận Quest đầu tiên.
        if (Status == QuestStatus.NotStarted)
        {
            return StartRandomQuest();
        }

        // Nếu đã Completed, chờ RewardManager trao thưởng
        // và tự động tạo Quest tiếp theo.
        return false;
    }

    // =========================================
    // START RANDOM QUEST
    // =========================================

    private bool StartRandomQuest()
    {
        if (!HasStateAuthority || questDatabase == null)
            return false;

        QuestSO quest = questDatabase.GetRandomQuest();

        if (quest == null)
        {
            Debug.LogWarning("[Quest] No valid Quest found.");
            return false;
        }

        return StartNewQuest(quest);
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

        // Validate toàn bộ Objective trước khi ghi Networked Data.
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

        // Xóa dữ liệu Objective cũ.
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
            $"[Quest] New Quest Started: {quest.questName}"
        );

        return true;
    }

    // =========================================
    // AUTO START NEXT QUEST AFTER REWARD
    // =========================================

    public bool TryStartNextQuestAfterReward()
    {
        if (!HasStateAuthority)
            return false;

        if (Status != QuestStatus.Completed)
            return false;

        if (questDatabase == null)
            return false;

        QuestSO nextQuest = GetNextRandomQuest();

        if (nextQuest == null)
        {
            Debug.LogWarning(
                "[Quest] Cannot find next valid Quest."
            );

            return false;
        }

        return StartNewQuest(nextQuest);
    }

    private QuestSO GetNextRandomQuest()
    {
        var quests = questDatabase.Quests;

        if (quests == null || quests.Count == 0)
            return null;

        // Đếm số Quest hợp lệ.
        int validCount = 0;

        foreach (QuestSO quest in quests)
        {
            if (!IsValidQuest(quest))
                continue;

            validCount++;
        }

        if (validCount == 0)
            return null;

        // Nếu chỉ có một Quest hợp lệ thì có thể random lại Quest đó.
        bool canExcludeCurrent = validCount > 1;

        int candidateCount = 0;

        foreach (QuestSO quest in quests)
        {
            if (!IsValidQuest(quest))
                continue;

            if (canExcludeCurrent &&
                quest.questID == CurrentQuestID)
                continue;

            candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        int randomIndex = Random.Range(0, candidateCount);

        foreach (QuestSO quest in quests)
        {
            if (!IsValidQuest(quest))
                continue;

            if (canExcludeCurrent &&
                quest.questID == CurrentQuestID)
                continue;

            if (randomIndex == 0)
                return quest;

            randomIndex--;
        }

        return null;
    }

    private bool IsValidQuest(QuestSO quest)
    {
        if (quest == null ||
            string.IsNullOrEmpty(quest.questID) ||
            quest.questID.Length > 32)
            return false;

        if (quest.objectives == null ||
            quest.objectives.Count == 0 ||
            quest.objectives.Count > 10)
            return false;

        return true;
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
    // CHECK COMPLETED
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
}
