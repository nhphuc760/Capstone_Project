using UnityEngine;
using Fusion;
using System;
using Game.Interaction;

/// <summary>
/// Quản lý hình ảnh item đang cầm trên tay Player.
///
/// Khi local player đổi Selected (ToolKit), HeldItemVisual nhận event,
/// gọi RPC gửi index lên mạng. Host ghi [Networked] HeldHotbarIndex,
/// mọi peer nhận OnChangedRender → bật/tắt visual theo ItemSO của slot đó.
/// </summary>
public class HeldItemVisual : NetworkBehaviour, IItemHoldProvider
{
    [Serializable]
    public class ItemVisualEntry
    {
        [Tooltip("ItemID trong ItemDatabase")]
        public ItemSO ItemSO;
        [Tooltip("Object 3D sẽ được bật khi cầm item này (nên là child của tay)")]
        public GameObject visualObject;
    }

    [Header("References")]
    [SerializeField] private NetworkInventory inventory;
    [SerializeField] private ToolKit toolKit;

    [Header("Visuals")]
    [Tooltip("Danh sách itemID → GameObject tương ứng.")]
    [SerializeField] private ItemVisualEntry[] visuals;

    [Header("Optional")]
    [SerializeField] private bool hideAllWhenEmpty = true;

    // Index hotbar đang cầm trên mạng (-1 = không cầm gì)
    [Networked, OnChangedRender(nameof(OnHeldIndexChangedRender))]
    private int HeldHotbarIndex { get; set; } = -1;

    public ItemSO CurrentHold => inventory.GetHotbarItemData(HeldHotbarIndex);

    private int _lastShownItemID = -1;

    public override void Spawned()
    {
        if (inventory == null)
            inventory = GetComponentInParent<NetworkInventory>() ?? GetComponent<NetworkInventory>();

        if (toolKit == null)
            toolKit = transform.root.GetComponentInChildren<ToolKit>(); // local UI thường chỉ 1 instance

        if (inventory == null)
            Debug.LogError($"{name}: Không tìm thấy NetworkInventory.");

        // Chỉ peer local (có InputAuthority) mới lắng nghe selection từ ToolKit
        if (Object.HasInputAuthority && toolKit != null)
            toolKit.OnLocalSelectedChanged += OnLocalSelectedChanged;

        if (inventory != null)
            inventory.OnInventoryChanged += OnInventoryDataChanged;

        // Client join muộn: áp dụng visual hiện tại
        RefreshVisualFromNetworkedIndex();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (toolKit != null)
            toolKit.OnLocalSelectedChanged -= OnLocalSelectedChanged;

        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryDataChanged;
    }

    /// <summary>
    /// Local ToolKit báo selection đã đổi → gửi index lên mạng.
    /// </summary>
    /// 
    // Sửa lại khi Use mới SetheldIndex
    private void OnLocalSelectedChanged(int index)
    {
        if (!Object.HasInputAuthority) return;

        if (Object.HasStateAuthority)
            SetHeldIndex(index);
        else
            RPC_SetHeldIndex(index);
    }

    private void SetHeldIndex(int index)
    {
        if (!Object.HasStateAuthority) return;
        if (index < -1 || index >= NetworkInventory.HotbarCapacity) return;

        // Slot trống → coi như không cầm
        if (index >= 0 && inventory != null && inventory.GetHotbarSlot(index).IsEmpty)
            index = -1;

        HeldHotbarIndex = index;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetHeldIndex(int index)
    {
        SetHeldIndex(index);
    }

    void OnHeldIndexChangedRender()
    {
        RefreshVisualFromNetworkedIndex();
    }

    /// <summary>
    /// Khi hotbar data đổi (swap/remove) → visual vẫn phải khớp item hiện tại của slot.
    /// </summary>
    private void OnInventoryDataChanged()
    {
        // Chỉ StateAuthority mới được sửa networked index khi slot bị trống
        if (Object.HasStateAuthority && HeldHotbarIndex >= 0)
        {
            if (inventory == null ||
                HeldHotbarIndex >= NetworkInventory.HotbarCapacity ||
                inventory.GetHotbarSlot(HeldHotbarIndex).IsEmpty)
            {
                HeldHotbarIndex = -1;
                return; // OnChangedRender sẽ gọi Refresh
            }
        }

        RefreshVisualFromNetworkedIndex();
    }

    private void RefreshVisualFromNetworkedIndex()
    {
        if (inventory == null)
        {
            ShowItem(0);
            return;
        }

        int index = HeldHotbarIndex;
        int targetItemID = 0;

        if (index >= 0 && index < NetworkInventory.HotbarCapacity)
        {
            var item = inventory.GetHotbarSlot(index);
            if (!item.IsEmpty)
                targetItemID = item.itemID;
        }

        if (targetItemID == _lastShownItemID) return;
        _lastShownItemID = targetItemID;

        ShowItem(targetItemID);
    }

    private void ShowItem(int itemID)
    {
        if (visuals == null) return;

        bool found = false;

        for (int i = 0; i < visuals.Length; i++)
        {
            var entry = visuals[i];
            if (entry.visualObject == null) continue;

            bool shouldShow = (itemID != 0 && entry.ItemSO.ItemId == itemID);
            entry.visualObject.SetActive(shouldShow);
            if (shouldShow) found = true;
        }

        if (!found && hideAllWhenEmpty)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i].visualObject != null)
                    visuals[i].visualObject.SetActive(false);
            }
        }
    }

    public void ForceRefresh()
    {
        _lastShownItemID = -999;
        RefreshVisualFromNetworkedIndex();
    }
}
