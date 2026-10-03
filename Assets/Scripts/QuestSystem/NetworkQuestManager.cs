using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
public class NetworkQuestManager : NetworkBehaviour
{
    // Đây là Quest ScriptableObject chứa thông tin nhiệm vụ
    [Header("Quest Data")]
    [SerializeField] private List<Quest> startingQuests;

    // Bật/tắt log Quest trong Console để kiểm tra khi debug
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // Quest đang hoạt động của player này
    private QuestProgress questProgress;

    // Tiến trình từng objective được đồng bộ qua Fusion
    [Networked, Capacity(16)]
    private NetworkArray<int> ObjectiveProgress => default;

    // ID của quest đang hoạt động
    [Networked, Capacity(64)]
    private NetworkString<_64> ActiveQuestID { get; set; }

    // Số lượng objective hiện tại
    [Networked]
    private int ObjectiveCount { get; set; }

    // Đã hoàn thành quest chưa
    [Networked]
    private NetworkBool IsQuestCompleted { get; set; }

    public Quest CurrentQuest => questProgress?.quest;

    public bool HasActiveQuest => questProgress != null && !IsQuestCompleted;

    public override void Spawned()
    {
        // Chỉ State Authority (thường là Host trong mô hình Host Mode)
        // được phép khởi tạo dữ liệu Quest Network.
        InitializeLocalQuest();
        if (Object.HasStateAuthority)
        {
            InitializeNetworkQuest();
        }
    }

    public override void Render()
    {
        // Cập nhật dữ liệu local từ Networked Properties
        if (questProgress == null)
            return;

        // Duyệt qua toàn bộ Objective.
        for (int i = 0; i < ObjectiveCount; i++)
        {
            // Tránh truy cập vượt quá số Objective thực tế
            if (i >= questProgress.objectives.Count)
                break;
            // Lấy tiến trình Objective từ NetworkArray và cập nhật vào QuestProgress local.
            questProgress.objectives[i].currentAmount = ObjectiveProgress[i];
        }
    }

    private void InitializeLocalQuest()
    {
        // Kiểm tra danh sách Quest có rỗng hay không.
        if (startingQuests == null || startingQuests.Count == 0)
        {
            Debug.LogWarning("Starting Quest list is empty!", this);
            return;
        }

        // Tạm thời lấy Quest đầu tiên để test
        Quest quest = startingQuests[0];

        questProgress = new QuestProgress(quest);
    }

    private void InitializeNetworkQuest()
    {
        // Nếu chưa tạo Quest local thì không làm gì.
        if (questProgress == null)
            return;

        // Lấy Quest hiện tại.
        Quest quest = questProgress.quest;

        // Lưu Quest ID lên Network.
        ActiveQuestID = quest.questID;

        // Lấy số lượng Objective, đảm bảo không vượt quá Capacity(16)     

        ObjectiveCount = Mathf.Min(questProgress.objectives.Count, 16);

        IsQuestCompleted = false;

        // Reset tiến trình của tất cả Objective về 0.
        for (int i = 0; i < ObjectiveCount; i++)
        {
            ObjectiveProgress.Set(i, 0);
        }

        if (debugLogs)
        {
            Debug.Log(
                $"Player {Object.InputAuthority} " +
                $"accepted quest: {quest.questName}"
            );
        }
    }

    // Gọi từ gameplay khi player thu thập vật phẩm
    public void CollectItem(string targetID, int amount = 1)
    {
        if (!HasInputAuthority)
            return;

        RPC_UpdateObjective(ObjectiveType.Collect, targetID, amount);
    }

    // Gọi từ gameplay khi player đánh bại quái nếu có đánh quái vật
    public void DefeatEnemy(string targetID,int amount = 1)
    {
        if (!HasInputAuthority)
            return;

        RPC_UpdateObjective(ObjectiveType.Defeat, targetID, amount);
    }

    // RPC này được gửi từ Player -> State Authority.   
    // State Authority là nơi quyết định tiến trình Quest có được cập nhật hay không.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateObjective(ObjectiveType type, string targetID, int amount)
    {
        if (IsQuestCompleted)
            return;

        if (amount <= 0)
            return;

        bool updated = false;
        // Duyệt qua tất cả Objective của Quest.
        for (int i = 0; i < ObjectiveCount; i++)
        {
            QuestObjective objective = questProgress.objectives[i];
            // Kiểm tra loại Objective nếu không đúng loại thì bỏ qua.
            if (objective.type != type)
                continue;

            // Kiểm tra đúng vật phẩm/quái vật
            if (objective.targetID != targetID)
                continue;

            // Nếu Objective này đã hoàn thành thì không cộng thêm.
            if (objective.IsCompleted)
                continue;

            // Tính số lượng mới.        
            // Mathf.Min giúp không vượt quá requiredAmount.                    
            // Ví dụ: hiện tại = 4            
            // yêu cầu = 5
            // nhặt thêm = 3           
            // kết quả = 5 chứ không phải 7.
            int newAmount = Mathf.Min(objective.currentAmount + amount,objective.requiredAmount);

            // Cập nhật tiến trình local.
            objective.currentAmount = newAmount;

            // Cập nhật tiến trình lên Network.
            ObjectiveProgress.Set(i, newAmount);

            updated = true;

            if (debugLogs)
            {
                Debug.Log($"Player {Object.InputAuthority} - " + $"{objective.description}: " + $"{newAmount}/{objective.requiredAmount}");
            }
        }

        // Nếu có Objective vừa được cập nhật và toàn bộ Quest đã hoàn thành
        if (updated && questProgress.IsCompleted())
        {
            // Đánh dấu Quest hoàn thành trên Network.
            IsQuestCompleted = true;

            if (debugLogs)
            {
                Debug.Log($"Quest completed: " + $"{questProgress.quest.questName}");
            }
        }
    }

    // Dùng để kiểm tra tiến trình trong UI
    public int GetObjectiveProgress(int index)
    {
        if (index < 0 || index >= ObjectiveCount)
            return 0;

        return ObjectiveProgress[index];
    }

    // Lấy số lượng cần thiết để hoàn thành Objective.
    // Ví dụ:
    // Collect 5 Copper Ore
    // => trả về 5.
    public int GetObjectiveRequiredAmount(int index)
    {
        if (questProgress == null)
            return 0;

        if (index < 0 || index >= questProgress.objectives.Count)
            return 0;


        return questProgress.objectives[index].requiredAmount;
    }
    // Lấy nội dung mô tả của Objective.   
    // Ví dụ:
    // "Collect 5 Copper Ore"  
    // UI Quest có thể dùng hàm này để hiển thị nhiệm vụ cho Player. 
    public string GetObjectiveDescription(int index)
    {
        if (questProgress == null)
            return string.Empty;

        if (index < 0 || index >= questProgress.objectives.Count)
            return string.Empty;

        return questProgress.objectives[index].description;
    }
    // Kiểm tra Quest đã hoàn thành chưa.
    public bool IsCompleted()
    {
        return IsQuestCompleted;
    }
}
