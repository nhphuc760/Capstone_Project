using Fusion;
using UnityEngine;

public enum QuestRewardType
{
    ValuableItem,
    GoldOre
}

public class NetworkQuestRewardManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkQuestSys questSystem;
    [SerializeField] private NetworkInventory inventory;

    [Header("Reward Settings")]
    [SerializeField] private QuestRewardType rewardType;

    [Header("Valuable Item Reward")]
    [SerializeField] private ItemSO rewardItem;
    [Min(1)]
    [SerializeField] private int rewardItemAmount;

    [Header("GoldOre Reward")]
    [Min(1)]
    [SerializeField] private int rewardGoldOre;

    [Header("Network State")]
    [Networked]
    private NetworkBool _rewardGranted { get; set; }

    #region Properties
    public bool IsRewardGranted => _rewardGranted;
    #endregion

    #region Spawned
    public override void Spawned()
    {
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (inventory == null)
            inventory = GetComponent<NetworkInventory>();
     

        if (HasStateAuthority)
        {
            Debug.Log(
                $"[QuestRewardManager] Spawned | " +
                $"Player={Object.InputAuthority}"
            );
        }
    }
    #endregion

    #region Fixed Update Network
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (_rewardGranted)
            return;

        if (questSystem == null)
            return;

        if (!questSystem.HasAssignedQuest)
            return;

        if (!questSystem.IsCompleted)
            return;

        GiveQuestReward();
    }
    #endregion

    #region Give Reward
    private void GiveQuestReward()
    {
        if (!HasStateAuthority)
            return;

        if (_rewardGranted)
            return;

        bool success = false;

        switch (rewardType)
        {
            case QuestRewardType.ValuableItem:

                success = GiveValuableItem();

                break;

            case QuestRewardType.GoldOre:
              

                break;
        }

        if (!success)
        {
            Debug.LogWarning(
                $"[QuestRewardManager] Failed to give reward | " +
                $"Player={Object.InputAuthority}"
            );

            return;
        }

        // Prevent duplicate rewards.
        _rewardGranted = true;

        Debug.Log(
            $"[QuestRewardManager] Reward Granted | " +
            $"Player={Object.InputAuthority} | " +
            $"Quest={questSystem.CurrentQuestID} | " +
            $"Reward={rewardType}"
        );
    }
    #endregion

    #region Valuable Item
    private bool GiveValuableItem()
    {
        if (inventory == null)
        {
            Debug.LogError(
                "[QuestRewardManager] NetworkInventory is missing!"
            );

            return false;
        }

        if (rewardItem == null)
        {
            Debug.LogError(
                "[QuestRewardManager] Reward Item is missing!"
            );

            return false;
        }

        if (rewardItem.ItemType != ItemType.Valuable)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Reward Item must be Valuable!"
            );

            return false;
        }

        if (rewardItemAmount <= 0)
            return false;

        bool added = inventory.TryAddItem(
            rewardItem.ItemId,
            rewardItemAmount
        );

        if (!added)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Inventory cannot receive reward!"
            );

            return false;
        }

        Debug.Log(
            $"[QuestRewardManager] Valuable Reward | " +
            $"Item={rewardItem.name} | " +
            $"Amount={rewardItemAmount}"
        );

        return true;
    }
    #endregion
   
}