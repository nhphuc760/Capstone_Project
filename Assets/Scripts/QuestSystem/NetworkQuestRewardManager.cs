
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkQuestRewardManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkQuestSys questSystem;
    [SerializeField] private NetworkInventory inventory;

    [Header("Reward Settings")]
    [SerializeField] private int maxRewardAmount = 1;

    [Networked] private NetworkBool _rewardChosen { get; set; }

    [Networked] private NetworkBool _rewardGranted { get; set; }

    [Networked] private int _rewardItemID { get; set; }

    [Networked] private int _rewardAmount { get; set; }

    // =========================================
    // SPAWNED
    // =========================================

    public override void Spawned()
    {
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (inventory == null)
            inventory = GetComponent<NetworkInventory>();
    }

    // =========================================
    // FIXED UPDATE NETWORK
    // =========================================

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (questSystem == null || inventory == null)
            return;

        if (questSystem.Status != QuestStatus.Completed)
            return;

        if (_rewardGranted)
            return;

        ProcessQuestReward();
    }

    // =========================================
    // PROCESS REWARD
    // =========================================

    private void ProcessQuestReward()
    {
        QuestSO quest = questSystem.Database.GetQuest(
            questSystem.CurrentQuestID
        );

        if (quest == null)
        {
            Debug.LogWarning(
                "[Quest Reward] Current Quest not found."
            );

            return;
        }

        // Chỉ random một lần cho Quest hiện tại.
        if (!_rewardChosen)
        {
            if (!TrySelectRandomReward(quest, out ItemSO reward))
            {
                Debug.LogWarning(
                    "[Quest Reward] No valid Valuable Item."
                );

                return;
            }

            _rewardItemID = reward.ItemId;

            _rewardAmount = Mathf.Max( 1, quest.rewardAmount);

            _rewardChosen = true;

            Debug.Log(
                $"[Quest Reward] Selected: {reward.name}, " +
                $"Amount: {_rewardAmount}"
            );
        }

        // Thử thêm phần thưởng vào Inventory.
        // Nếu thất bại, giữ nguyên ID đã random để thử lại.
        bool added = inventory.TryAddItem(
            _rewardItemID,
            _rewardAmount
        );

        if (!added)
        {
            Debug.LogWarning(
                $"[Quest Reward] Cannot add Item ID: {_rewardItemID}. " +
                "Will retry."
            );

            return;
        }

        // Đánh dấu đã trao thưởng.
        _rewardGranted = true;

        Debug.Log(
            $"[Quest Reward] Successfully added Item ID: " +
            $"{_rewardItemID} x{_rewardAmount}"
        );

        // Sau khi trao thưởng thành công,
        // tự động tạo Quest tiếp theo.
        bool nextQuestStarted =
            questSystem.TryStartNextQuestAfterReward();

        if (nextQuestStarted)
        {
            // Reset trạng thái Reward để chuẩn bị Quest mới.
            _rewardChosen = false;
            _rewardGranted = false;
            _rewardItemID = 0;
            _rewardAmount = 0;

            Debug.Log(
                $"[Quest Reward] Next Quest Started: " +
                $"{questSystem.CurrentQuestID}"
            );
        }
        else
        {
            // Không tìm được Quest tiếp theo.
            // Giữ _rewardGranted = true để không trao thưởng lặp.
            Debug.LogWarning(
                "[Quest Reward] Reward granted, " +
                "but no next Quest could be started."
            );
        }
    }

    // =========================================
    // RANDOM VALUABLE REWARD
    // =========================================

    private bool TrySelectRandomReward(
        QuestSO quest,
        out ItemSO selectedReward)
    {
        selectedReward = null;

        if (quest.rewardItems == null ||
            quest.rewardItems.Length == 0)
            return false;

        List<ItemSO> validRewards = new List<ItemSO>();

        foreach (ItemSO item in quest.rewardItems)
        {
            if (item == null)
                continue;

            if (item.ItemType != ItemType.Valuable)
                continue;

            validRewards.Add(item);
        }

        if (validRewards.Count == 0)
            return false;

        int randomIndex = Random.Range(
            0,
            validRewards.Count
        );

        selectedReward = validRewards[randomIndex];

        return true;
    }
}
