
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkQuestRewardManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkQuestSys questSystem;
    [SerializeField] private NetworkInventory inventory;

    [Header("Reward Settings")]
    [SerializeField] private bool enableDebugLog = true;

    // Trạng thái phần thưởng
    [Networked] private NetworkBool _rewardChosen { get; set; }
    [Networked] private NetworkBool _rewardGranted { get; set; }

    // Thông tin phần thưởng đã chọn
    [Networked] private int _rewardItemID { get; set; }
    [Networked] private int _rewardAmount { get; set; }

    public override void Spawned()
    {
        // Tự tìm component nếu chưa được gán trong Inspector
        if (questSystem == null)
            questSystem = GetComponent<NetworkQuestSys>();

        if (inventory == null)
            inventory = GetComponent<NetworkInventory>();

        if (enableDebugLog)
        {
            Debug.Log(
                $"[Quest Reward] Spawned | " +
                $"StateAuthority: {HasStateAuthority} | " +
                $"QuestSystem: {questSystem != null} | " +
                $"Inventory: {inventory != null}"
            );
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ StateAuthority được xử lý phần thưởng
        if (!HasStateAuthority)
            return;

        if (questSystem == null || inventory == null)
            return;

        if (questSystem.Database == null)
            return;

        // Chỉ xử lý khi Quest đã hoàn thành
        if (questSystem.Status != QuestStatus.Completed)
            return;

        // Tránh trao thưởng nhiều lần
        if (_rewardGranted)
            return;

        ProcessQuestReward();
    }

    private void ProcessQuestReward()
    {
        // Lấy Quest hiện tại từ Database
        QuestSO quest = questSystem.Database.GetQuest(
            questSystem.CurrentQuestID
        );

        if (quest == null)
        {
            Debug.LogWarning(
                "[Quest Reward] Cannot find current Quest in Database."
            );
            return;
        }

        // Bước 1: Chọn phần thưởng nếu chưa chọn

        if (!_rewardChosen)
        {

            if (!TrySelectRandomReward(quest, out ItemSO reward))
            {
                Debug.LogWarning(
                    $"[Quest Reward] Quest '{quest.questName}' " +
                    "does not have a valid Valuable reward."
                );
                return;
            }

            // Lưu thông tin phần thưởng lên Networked Properties
            _rewardItemID = reward.ItemId;
            _rewardAmount = Mathf.Max(1, quest.rewardAmount);

            _rewardChosen = true;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[Quest Reward] Selected Reward: {reward.name} | " +
                    $"Item ID: {_rewardItemID} | " +
                    $"Amount: {_rewardAmount}"
                );
            }
        }

        // Bước 2: Thêm phần thưởng vào Inventory
        bool added = inventory.TryAddItem(
            _rewardItemID,
            _rewardAmount
        );

        // Nếu Inventory đầy, giữ nguyên phần thưởng đã chọn
        // và thử thêm lại ở lần FixedUpdateNetwork tiếp theo
        if (!added)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning(
                    $"[Quest Reward] Cannot add Item ID: {_rewardItemID}. " +
                    "Inventory may be full. Retrying..."
                );
            }

            return;
        }

        // Bước 3: Đánh dấu đã trao thưởng
        _rewardGranted = true;

        if (enableDebugLog)
        {
            Debug.Log(
                $"[Quest Reward] Successfully Granted | " +
                $"Item ID: {_rewardItemID} | " +
                $"Amount: {_rewardAmount}"
            );
        }

        // Bước 4: Chuyển sang Quest tiếp theo
        bool nextQuestStarted =
            questSystem.TryStartNextQuestAfterReward();

        if (nextQuestStarted)
        {
            // Reset dữ liệu phần thưởng cho Quest tiếp theo
            ResetRewardData();

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[Quest Reward] Next Quest Started: " +
                    $"{questSystem.CurrentQuestID}"
                );
            }
        }
        else
        {
            // Không còn Quest tiếp theo
            // Giữ _rewardGranted = true để tránh trao thưởng lại
            if (enableDebugLog)
            {
                Debug.Log(
                    "[Quest Reward] No more Quest available. " +
                    "Quest progression finished."
                );
            }
        }
    }

    private bool TrySelectRandomReward(
        QuestSO quest,
        out ItemSO selectedReward
    )
    {
        selectedReward = null;

        if (quest == null)
            return false;

        if (quest.rewardItems == null ||
            quest.rewardItems.Length == 0)
        {
            return false;
        }

        List<ItemSO> validRewards = new List<ItemSO>();

        // Lọc những vật phẩm hợp lệ
        foreach (ItemSO item in quest.rewardItems)
        {
            if (item == null)
                continue;

            // Chỉ nhận Valuable Item
            if (item.ItemType != ItemType.Consumable)
                continue;

            // Item ID phải hợp lệ
            if (item.ItemId <= 0)
                continue;

            validRewards.Add(item);
        }

        if (validRewards.Count == 0)
            return false;

        // Random một phần thưởng trong danh sách hợp lệ
        int randomIndex = Random.Range(0, validRewards.Count);

        selectedReward = validRewards[randomIndex];

        return true;
    }

    private void ResetRewardData()
    {
        _rewardChosen = false;
        _rewardGranted = false;

        _rewardItemID = 0;
        _rewardAmount = 0;
    }
}
