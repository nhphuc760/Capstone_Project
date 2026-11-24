
using Fusion;
using UnityEngine;

public class PlayerQuestTracker : NetworkBehaviour
{
    #region REFERENCES

    [Header("Quest Reference")]
    [SerializeField] private NetworkQuestSys questSystem;

    #endregion

    #region GUI SETTINGS

    [Header("GUI Settings")]
    [SerializeField] private Vector2 guiPosition = new Vector2(10f, 110f);
    [SerializeField] private float guiWidth = 590f;
    [SerializeField] private float lineHeight = 30f;
    [SerializeField] private float padding = 12f;

    [Header("Display Settings")]
    [SerializeField] private bool showQuestID = false;
    [SerializeField] private bool showObjectiveProgress = true;

    private GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle labelStyle;
    private GUIStyle statusStyle;
    private GUIStyle objectiveStyle;
    private GUIStyle progressStyle;

    #endregion

    #region SPAWNED

    public override void Spawned()
    {
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (questSystem == null)
        {
            Debug.LogError(
                "[PlayerQuestTracker] NetworkQuestSys not found on Player!"
            );
            return;
        }

        if (Object.HasInputAuthority)
        {
            Debug.Log("[PlayerQuestTracker] Initialized for LocalPlayer.");
        }
    }

    #endregion

    #region TEST INPUT

    private void Update()
    {
        // Chỉ LocalPlayer mới được sử dụng phím test
        if (Object == null || !Object.HasInputAuthority)
            return;

        // Nhấn phím số 1 để hoàn thành Quest
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RPC_CompleteQuestForTest();
        }
    }

    #endregion

    #region RPC COMPLETE QUEST

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CompleteQuestForTest()
    {
        if (!HasStateAuthority)
            return;

        if (questSystem == null)
            return;

        // Chỉ hoàn thành Quest đang thực hiện
        if (questSystem.Status != QuestStatus.InProgress)
        {
            Debug.Log("[QuestTracker] No quest is in progress!");
            return;
        }

        Debug.Log("[QuestTracker] Completing current quest...");

        for (int i = 0; i < questSystem.ObjectiveCount; i++)
        {
            NetworkObjective objective = questSystem.GetObjective(i);

            if (objective.Completed)
                continue;

            int remainingAmount = Mathf.Max(
                0,
                objective.RequiredAmount - objective.CurrentAmount
            );

            if (remainingAmount <= 0)
                continue;

            // Collect Objective
            if ((ObjectiveType)objective.Type == ObjectiveType.Collect)
            {
                questSystem.AddCollectProgress(
                    objective.TargetItemID,
                    remainingAmount
                );
            }

            // Defeat Objective
            else if ((ObjectiveType)objective.Type == ObjectiveType.Defeat)
            {
                questSystem.AddDefeatProgress(
                    objective.TargetID.ToString(),
                    remainingAmount
                );
            }
        }

        Debug.Log("[QuestTracker] Quest completion test finished!");
    }

#endregion

    #region GUI

    private void OnGUI()
    {
        // Chỉ hiển thị Quest của LocalPlayer
        if (Object == null || !Object.HasInputAuthority)
            return;

        if (questSystem == null)
            return;

        InitializeStyles();

        float x = guiPosition.x + padding;
        float y = guiPosition.y + padding;
        float contentWidth = guiWidth - padding * 2f;

        float panelHeight = CalculatePanelHeight();

        GUI.Box(
            new Rect(
                guiPosition.x,
                guiPosition.y,
                guiWidth,
                panelHeight
            ),
            ""
        );

        #region TITLE

        GUI.Label(
            new Rect(x, y, contentWidth, lineHeight + 5f),
            "CURRENT QUEST",
            titleStyle
        );

        y += lineHeight + 8f;

        #endregion

        #region QUEST STATUS

        DrawStatus(x, ref y, contentWidth);

        if (questSystem.Status == QuestStatus.NotStarted)
        {
            DrawLabel(
                "Waiting for quest assignment...",
                x, ref y, contentWidth
            );

            return;
        }

        #endregion

        #region QUEST INFORMATION

        QuestSO quest = questSystem.Database != null
            ? questSystem.Database.GetQuest(
                questSystem.CurrentQuestID
              )
            : null;

        if (showQuestID)
        {
            DrawLabel(
                $"Quest ID: {questSystem.CurrentQuestID}",
                x, ref y, contentWidth
            );
        }

        if (quest != null)
        {
            DrawLabel(
                $"Name: {quest.questName}",
                x, ref y, contentWidth
            );
        }

        y += 5f;

        #endregion

        #region OBJECTIVES

        DrawSectionTitle(
            "OBJECTIVES",
            x, ref y, contentWidth
        );

        for (int i = 0; i < questSystem.ObjectiveCount; i++)
        {
            NetworkObjective objective =
                questSystem.GetObjective(i);

            string objectiveID =
                objective.ObjectiveID.ToString();

            string description = objectiveID;

            // Tìm Description trong QuestSO
            if (quest != null && quest.objectives != null)
            {
                QuestObjective data = quest.objectives.Find(
                    item => item != null &&
                            item.objectiveID == objectiveID
                );

                if (data != null)
                    description = data.description;
            }

            // Objective status
            string statusText = objective.Completed
                ? "[DONE]"
                : "[IN PROGRESS]";

            GUI.Label(
                new Rect(x, y, contentWidth, lineHeight),
                $"{statusText} {description}",
                objectiveStyle
            );

            y += lineHeight;

            // Objective progress
            if (showObjectiveProgress)
            {
                DrawProgress(
                    objective.CurrentAmount,
                    objective.RequiredAmount,
                    x, ref y, contentWidth
                );
            }

            y += 5f;
        }

        #endregion
    }

    #endregion

    #region PANEL HEIGHT

    private float CalculatePanelHeight()
    {
        float height = padding * 2f + lineHeight + 8f;

        // Quest Status
        height += lineHeight;

        if (questSystem.Status == QuestStatus.NotStarted)
        {
            height += lineHeight;
            return height;
        }

        if (showQuestID)
            height += lineHeight;

        QuestSO quest = questSystem.Database != null
            ? questSystem.Database.GetQuest(
                questSystem.CurrentQuestID
              )
            : null;

        // Quest Name
        if (quest != null)
            height += lineHeight;

        // Khoảng cách trước Objectives
        height += 5f;

        // Objectives Title
        height += lineHeight + 5f;

        // Objective content
        height += questSystem.ObjectiveCount *
                  (lineHeight + 5f);

        // Progress
        if (showObjectiveProgress)
        {
            height += questSystem.ObjectiveCount *
                      lineHeight;
        }

        return height + padding;
    }

    #endregion

    #region DRAW STATUS

    private void DrawStatus(
        float x,
        ref float y,
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

        y += lineHeight;
    }

    #endregion

    #region DRAW OBJECTIVE PROGRESS

    private void DrawProgress(
        int current,
        int required,
        float x,
        ref float y,
        float width)
    {
        string progressText = $"Progress: {current}/{required}";

        GUI.Label(
            new Rect(x + 15f, y, width - 15f, lineHeight),
            progressText,
            progressStyle
        );

        y += lineHeight;
    }

    #endregion

    #region DRAW LABEL

    private void DrawLabel(
        string message,
        float x,
        ref float y,
        float width)
    {
        GUI.Label(
            new Rect(x, y, width, lineHeight),
            message,
            labelStyle
        );

        y += lineHeight;
    }

    #endregion

    #region DRAW SECTION TITLE

    private void DrawSectionTitle(
        string message,
        float x,
        ref float y,
        float width)
    {
        GUI.Label(
            new Rect(x, y, width, lineHeight),
            message,
            sectionStyle
        );

        y += lineHeight + 5f;
    }

    #endregion

    #region GUI STYLE

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        // Main Title
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        // Section Title
        sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        // Normal Label
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        // Quest Status
        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green }
        };

        // Objective
        objectiveStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        // Progress
        progressStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
    }

    #endregion
}
