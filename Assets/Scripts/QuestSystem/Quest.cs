using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    
    public string questName;
    public string description;
    public List<QuestObjective> objectives;

    // Hàm này được Unity gọi khi ScriptableObject được chỉnh sửa trong Inspector.    
    private void OnValidate()
    {
        if(string.IsNullOrEmpty(questID))
        {
            questID = questName + Guid.NewGuid().ToString();
        }
    }  
}

// [Serializable] cho phép Unity hiển thị class này bên trong Inspector.
[Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string targetID;
    public string description;
    public ObjectiveType type;
    public int requiredAmount;
    public int currentAmount;

    public bool IsCompleted => currentAmount >= requiredAmount;
}

// Xác định loại nhiệm vụ của Objective.
public enum ObjectiveType { Collect, Defeat }

// Lưu tiến trình của một Quest trong lúc Player đang thực hiện Quest. 
[Serializable]
public class QuestProgress
{
    public Quest quest;
    public List<QuestObjective> objectives;

    public QuestProgress(Quest quest)
    {
        // Lưu Quest ScriptableObject gốc.
        this.quest = quest;
        objectives = new List<QuestObjective>();

        foreach (var obj in quest.objectives)
        {
            // Tạo một QuestObjective mới.
            // Không sử dụng trực tiếp Objective gốc trong ScriptableObject.         
            // Điều này giúp mỗi Player có tiến trình riêng.
            objectives.Add(new QuestObjective
            {
                objectiveID = obj.objectiveID,
                targetID = obj.targetID,
                description = obj.description,
                type = obj.type,
                requiredAmount = obj.requiredAmount,
                currentAmount = 0
            });
        }
    }

    // Kiểm tra toàn bộ Objective của Quest đã hoàn thành hay chưa.
    // TrueForAll() nghĩa là: Tất cả Objective đều phải IsCompleted = true.
    // 
    public bool IsCompleted() => objectives.TrueForAll(o => o.IsCompleted);

    // Trả về questID của Quest hiện tại.
    public string QuestID => quest.questID;
}
