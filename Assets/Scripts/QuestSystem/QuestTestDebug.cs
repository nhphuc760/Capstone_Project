using UnityEngine;

public class QuestTestDebug : MonoBehaviour
{
    [SerializeField] private NetworkQuestManager questManager;

    private void Update()
    {
        if (questManager == null)
            return;

        if (!questManager.HasInputAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            // Giả lập nhặt Copper Ore
            questManager.CollectItem("copper_ore", 1);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            // Giả lập nhặt Iron Ore
            questManager.CollectItem("iron_ore", 1);
        }
    }
}
