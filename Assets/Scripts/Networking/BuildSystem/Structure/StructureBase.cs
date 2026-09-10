using System;
using System.Linq;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public abstract class StructureBase : NetworkBehaviour
{
    [Networked]
    public int Level {  get; set; }
    [Networked] 
    NetworkBool IsUpgradeable {  get; set; }
    public StructureDataSO StructureDataSO;
    StructureUpgradeData _currentUpgradeData;
    //public StructureManager Manager { get; set; }
    public abstract void Operation();  

    public override void Spawned()
    {
        base.Spawned();

        Level = 1;
        IsUpgradeable = false;
        _currentUpgradeData = null;

        if (StructureDataSO == null || StructureDataSO.levels == null || StructureDataSO.levels.Length == 0)
            return;

        var firstLevelData = StructureDataSO.levels[0];
        if (firstLevelData == null || firstLevelData.upgradeRequirement == null)
            return;

        _currentUpgradeData = firstLevelData.upgradeRequirement.CreateInstance(this) as StructureUpgradeData;
        IsUpgradeable = _currentUpgradeData != null;
    }

    public UpgradeResult CanUpgrade()
    {
        if (StructureDataSO == null || StructureDataSO.levels == null || StructureDataSO.levels.Length == 0)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.NonUpgradable,
                Message = "Công trình này không thể nâng cấp"
            };
        }



        if (Level >= StructureDataSO.levels.Length)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.MaxLevel,
                Message = "Công trình đã đạt cấp tối đa"
            };
        }
        if (Level < 1)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.NonUpgradable,
                Message = "Level không hợp lệ"
            };
        }

        var nextLevelData = StructureDataSO.levels[Level]; // index = Level (vì Level bắt đầu từ 1)
        if (nextLevelData == null || nextLevelData.upgradeRequirement == null)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.NonUpgradable,
                Message = "Không có dữ liệu nâng cấp cho cấp tiếp theo"
            };
        }

        // 5. Chưa khởi tạo được _currentUpgradeData
        if (_currentUpgradeData == null)
        {
            return new UpgradeResult
            {
                Reason = UpgradeFailReason.NonUpgradable,
                Message = "Không thể nâng cấp"
            };
        }

        return _currentUpgradeData.CheckRequirement();
    }
    /// <summary>
    /// Nâng cấp an toàn. Chỉ chạy khi CanUpgrade() thành công.
    /// </summary>
    public void Upgrade()
    {
        if (!Object.HasStateAuthority)
            return;

        UpgradeResult result = CanUpgrade();
        if (!result.Success)
        {
            Debug.LogWarning($"[StructureBase] Không thể nâng cấp: {result.Message} ({result.Reason})");
            return;
        }

        // Tăng level
        Level++;

        // Hủy requirement cũ
        if (_currentUpgradeData != null)
        {
            _currentUpgradeData.Destroy();
            _currentUpgradeData = null;
        }

        // Gán requirement mới cho level vừa đạt được (nếu còn level tiếp theo)
        if (Level < StructureDataSO.levels.Length)
        {
            var nextLevelData = StructureDataSO.levels[Level];
            if (nextLevelData != null && nextLevelData.upgradeRequirement != null)
            {
                _currentUpgradeData = nextLevelData.upgradeRequirement.CreateInstance(this) as StructureUpgradeData;
                IsUpgradeable = _currentUpgradeData != null;
            }
            else
            {
                IsUpgradeable = false;
            }
        }
        else
        {
            // Đã max level
            IsUpgradeable = false;
        }

        // Gọi logic nâng cấp riêng của từng loại công trình
        UpgradeLogic();
    }

    public virtual void Destroy()
    {
        //if (HasStateAuthority) Runner.Despawn(this.Object);
    }


    public abstract void UpgradeLogic();

    private void OnMouseDown()
    {
        if (Runner.LocalPlayer != Object.InputAuthority) return;
        var strategy = StructureManager.Ins.GetStrategyBuild(StructureDataSO._id);
        Debug.Log("Strategy: " + strategy != null ? strategy.GetType().Name : "null");
        strategy.Destroy(Runner, Object);
    }
}


public abstract class StructAttackBase: StructureBase
{
    public event Action<int> OnDamage;

}


