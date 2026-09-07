using System;
using System.Collections.Generic;
using System.Linq;


[Serializable]
public class Stats
{

    StatsBase baseStats;

    readonly Dictionary<StatsType, List<IModifier>> _modifiers = new();
    readonly Dictionary<StatsType, int> _cache = new();
    readonly HashSet<StatsType> _dirty = new HashSet<StatsType>();

    // Danh sách riêng để dễ update
    private readonly List<TimeModifier> _timedModifiers = new();



    public Stats(StatsBase baseStats)
    {
        this.baseStats = baseStats;
        
    }



    public int Get(StatsType type)
    {
        if (!_dirty.Contains(type) && _cache.TryGetValue(type, out int cached)) return cached;

        int baseValue = GetBase(type);
        int value = baseValue;

        if (_modifiers.TryGetValue(type, out var list) && list.Count > 0)
        {
            foreach (var mod in list.OrderBy(m => m.Priority))
            {
                value = mod.Apply(baseValue, value);
            }

        }

        _cache[type] = value;
        _dirty.Remove(type);
        return value;

    }


    public int GetBase(StatsType type)
    {
        return baseStats.stats.TryGetValue(type, out int v) ? v : 0;
    }

    public void SetBase(StatsType type, int value)
    {
        baseStats.stats[type] = value;
        MarkDirty(type);
    }

    public void AddModifier(IModifier modifier)
    {
        if (!_modifiers.TryGetValue(modifier.TargetStat, out var list))
        {
            list = new List<IModifier>();
            _modifiers[modifier.TargetStat] = list;
        }

        list.Add(modifier);
        MarkDirty(modifier.TargetStat);

        // Nếu là TimedModifier thì theo dõi riêng
        if (modifier is TimeModifier timed)
            _timedModifiers.Add(timed);
    }

    public void RemoveModifier(IModifier modifier)
    {
        if (_modifiers.TryGetValue(modifier.TargetStat, out var list))
        {
            if (list.Remove(modifier))
                MarkDirty(modifier.TargetStat);
        }

        if (modifier is TimeModifier timed)
            _timedModifiers.Remove(timed);
    }

    public void ClearAllModifiers()
    {
        _modifiers.Clear();
        _dirty.Clear();
        _cache.Clear();
    }

    void MarkDirty(StatsType type)
    {
        _dirty.Add(type);
        _cache.Remove(type);
    }

    /// <summary>
    /// Phải gọi mỗi frame (hoặc trong Update của Character)
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_timedModifiers.Count == 0) return;

        // Duyệt ngược để xóa an toàn
        for (int i = _timedModifiers.Count - 1; i >= 0; i--)
        {
            var timed = _timedModifiers[i];
            if (timed.Tick(deltaTime))
            {
                // Hết hạn → xóa khỏi hệ thống
                RemoveModifier(timed);
            }
        }
    }
}