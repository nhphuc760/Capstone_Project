
using Fusion;
using UnityEngine;

public class SpawnTest : NetworkBehaviour
{
    [Header("Quest Reference")]
    [SerializeField] private NetworkQuestSys questSystem;
    [SerializeField] private NetworkQuestRewardManager rewardManager;

    [Header("GUI Settings")]
    [SerializeField] private Vector2 guiPosition = new Vector2(20, 20);
    [SerializeField] private float guiWidth = 420f;
    [SerializeField] private float lineHeight = 25f;

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle statusStyle;

    private bool requestSent;

    // =========================================
    // SPAWNED
    // =========================================

    public override void Spawned()
    {
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (rewardManager == null)
            rewardManager = GetComponent<NetworkQuestRewardManager>();

        if (Object.HasInputAuthority && !requestSent)
        {
            questSystem.RequestStartOrContinueQuest();
            requestSent = true;
        }
    }

    // =========================================
    // GUI
    // =========================================

    private void OnGUI()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        if (questSystem == null)
            return;

        InitializeStyles();

        float panelHeight = 160f +
            questSystem.ObjectiveCount * lineHeight * 2f;

        GUI.Box(
            new Rect(
                guiPosition.x,
                guiPosition.y,
                guiWidth,
                panelHeight
            ),
            ""
        );

        float x = guiPosition.x + 12f;
        float y = guiPosition.y + 10f;
        float contentWidth = guiWidth - 24f;

        GUI.Label(
            new Rect(x, y, contentWidth, lineHeight),
            "PLAYER QUEST",
            titleStyle
        );

        y += lineHeight + 5f;

        if (questSystem.Status == QuestStatus.NotStarted)
        {
            DrawLabel(
                "Quest: Waiting for assignment...",
                x, y, contentWidth
            );

            return;
        }

        QuestSO quest = questSystem.Database != null
            ? questSystem.Database.GetQuest(
                questSystem.CurrentQuestID
              )
            : null;

        DrawLabel(
            $"Quest ID: {questSystem.CurrentQuestID}",
            x, y, contentWidth
        );

        y += lineHeight;

        if (quest != null)
        {
            DrawLabel(
                $"Name: {quest.questName}",
                x, y, contentWidth
            );

            y += lineHeight;

            DrawLabel(
                $"Description: {quest.description}",
                x, y, contentWidth
            );

            y += lineHeight;
        }

        DrawStatus(x, y, contentWidth);

        y += lineHeight + 5f;

        DrawLabel(
            "OBJECTIVES:",
            x, y, contentWidth
        );

        y += lineHeight;

        for (int i = 0; i < questSystem.ObjectiveCount; i++)
        {
            NetworkObjective objective =
                questSystem.GetObjective(i);

            string objectiveID =
                objective.ObjectiveID.ToString();

            string description = objectiveID;

            if (quest != null)
            {
                QuestObjective data = quest.objectives.Find(
                    item => item.objectiveID == objectiveID
                );

                if (data != null)
                    description = data.description;
            }

            string objectiveStatus = objective.Completed
                ? "[DONE]"
                : "[IN PROGRESS]";

            DrawLabel(
                $"{objectiveStatus} {description}",
                x, y, contentWidth
            );

            y += lineHeight;

            DrawLabel(
                $"Progress: {objective.CurrentAmount}/" +
                $"{objective.RequiredAmount}",
                x, y, contentWidth
            );

            y += lineHeight;
        }
    }

    
private void Update()
{
    // Only the local player can trigger the test
    if (Object == null || !Object.HasInputAuthority)
        return;

    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        RequestCompleteQuestForTest();
    }
}

private void RequestCompleteQuestForTest()
{
    if (questSystem == null)
        return;

    // Host / StateAuthority can execute directly
    if (Object.HasStateAuthority)
    {
        CompleteQuestForTest();
    }
    else
    {
        RPC_CompleteQuestForTest();
    }
}

[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
private void RPC_CompleteQuestForTest()
{
    CompleteQuestForTest();
}

private void CompleteQuestForTest()
{
    if (!Object.HasStateAuthority)
        return;

    if (questSystem == null || rewardManager == null)
    {
        Debug.LogWarning("[QuestTest] Missing QuestSystem or RewardManager!");
        return;
    }

    if (questSystem.Status != QuestStatus.InProgress)
    {
        Debug.Log("[QuestTest] No active quest to complete.");
        return;
    }

    Debug.Log("[QuestTest] Completing quest...");

    for (int i = 0; i < questSystem.ObjectiveCount; i++)
    {
        NetworkObjective objective = questSystem.GetObjective(i);

        if (objective.Completed)
            continue;

        int remainingAmount =
            objective.RequiredAmount - objective.CurrentAmount;

        if (remainingAmount <= 0)
            continue;

        ObjectiveType type = (ObjectiveType)objective.Type;

        switch (type)
        {
            case ObjectiveType.Collect:
                questSystem.AddCollectProgress(
                    objective.TargetItemID,
                    remainingAmount
                );
                break;

            case ObjectiveType.Defeat:
                questSystem.AddDefeatProgress(
                    objective.TargetID.ToString(),
                    remainingAmount
                );
                break;
        }
    }

    Debug.Log("[QuestTest] Quest completion requested.");
    Debug.Log("[QuestTest] Waiting for NetworkQuestRewardManager...");
}


    // =========================================
    // DRAW STATUS
    // =========================================

    private void DrawStatus(
        float x,
        float y,
        float width)
    {
        string statusText;

        switch (questSystem.Status)
        {
            case QuestStatus.NotStarted:
                statusText = "NOT STARTED";
                break;

            case QuestStatus.InProgress:
                statusText = "IN PROGRESS";
                break;

            case QuestStatus.Completed:
                statusText = "COMPLETED";
                break;

            default:
                statusText = "UNKNOWN";
                break;
        }

        GUI.Label(
            new Rect(x, y, width, lineHeight),
            $"Status: {statusText}",
            statusStyle
        );
    }

    // =========================================
    // DRAW LABEL
    // =========================================

    private void DrawLabel(
        string message,
        float x,
        float y,
        float width)
    {
        GUI.Label(
            new Rect(x, y, width, lineHeight),
            message,
            labelStyle
        );
    }

    // =========================================
    // GUI STYLE
    // =========================================

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green }
        };
    }
}
