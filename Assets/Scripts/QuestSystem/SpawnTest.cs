
using Fusion;
using UnityEngine;

public class SpawnTest : NetworkBehaviour
{
    #region REFERENCES

    [Header("Quest Reference")]
    [SerializeField] private NetworkQuestSys questSystem;
    [SerializeField] private NetworkQuestRewardManager rewardManager;

    #endregion

    #region GUI SETTINGS

    [Header("GUI Settings")]
    [SerializeField] private Vector2 guiPosition = new Vector2(20, 20);
    [SerializeField] private float guiWidth = 420f;
    [SerializeField] private float lineHeight = 25f;

    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle statusStyle;
    private GUIStyle buttonStyle;

    #endregion

    #region TEST SETTINGS

    private bool requestSent;

    #endregion

    #region SPAWNED

    public override void Spawned()
    {
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (rewardManager == null)
            rewardManager = GetComponent<NetworkQuestRewardManager>();

        Debug.Log(
            $"[SpawnTest] Spawned | InputAuthority: {Object.HasInputAuthority}"
        );
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        if (questSystem == null)
            return;

        InitializeStyles();

        float panelHeight = 270f +
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

        #region QUEST BUTTONS

        if (GUI.Button(
            new Rect(x, y, contentWidth, 32f),
            "ACCEPT FIXED QUEST",
            buttonStyle))
        {
            RequestAcceptFixedQuestForTest();
        }

        y += 38f;

        GUI.enabled = questSystem.Status == QuestStatus.InProgress;

        if (GUI.Button(
            new Rect(x, y, contentWidth, 32f),
            "COMPLETE QUEST",
            buttonStyle))
        {
            RequestCompleteQuestForTest();
        }

        GUI.enabled = true;

        y += 42f;

        #endregion

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

    #endregion

    #region UPDATE - KEYBOARD TEST

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        // Phím 1: Hoàn thành Quest
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RequestCompleteQuestForTest();
        }

        // Phím N: Tương tác NPC
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (SnowmanNPC.instance != null)
                SnowmanNPC.instance.Interact();
        }
    }

    #endregion

    #region ACCEPT FIXED QUEST TEST

    private void RequestAcceptFixedQuestForTest()
    {
        if (questSystem == null)
        {
            Debug.LogWarning("[QuestTest] QuestSystem is missing!");
            return;
        }

        Debug.LogWarning("[QuestTest] Requesting Fixed Quest...");

        questSystem.RequestAcceptFixedQuest();
    }

    #endregion

    #region COMPLETE QUEST TEST

    private void RequestCompleteQuestForTest()
    {
        if (questSystem == null)
            return;

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
            Debug.LogWarning(
                "[QuestTest] Missing QuestSystem or RewardManager!"
            );
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
            NetworkObjective objective =
                questSystem.GetObjective(i);

            if (objective.Completed)
                continue;

            int remainingAmount =
                objective.RequiredAmount - objective.CurrentAmount;

            if (remainingAmount <= 0)
                continue;

            ObjectiveType type =
                (ObjectiveType)objective.Type;

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

    #endregion

    #region DRAW STATUS

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

    #endregion

    #region DRAW LABEL

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

    #endregion

    #region GUI STYLE

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

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
    }

    #endregion
}
