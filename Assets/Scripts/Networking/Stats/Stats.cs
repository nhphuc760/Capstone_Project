using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEditor;
using UnityEngine;


[Serializable]
public class Stats
{
    readonly ModifierDatabaseSO modifierDatabaseSO;
    StatsBase baseStats;

    readonly Dictionary<StatsType, List<IModifier>> _modifiers = new();
    readonly Dictionary<StatsType, int> _cache = new();
    readonly HashSet<StatsType> _dirty = new HashSet<StatsType>();
    
    // Danh sách riêng để dễ update
    private readonly List<TimeModifier> _timedModifiers = new();

    


    public Stats(StatsBase baseStats, ModifierDatabaseSO modifierDatabaseSO)
    {
        this.baseStats = baseStats;
        this.modifierDatabaseSO = modifierDatabaseSO;
    }



    public int Get(StatsType type)
    {
        if (!_dirty.Contains(type) && _cache.TryGetValue(type, out int cached)) return cached;

        int baseValue = GetBase(type);
        int value = baseValue;

        if (_modifiers.TryGetValue(type, out var list) && list.Count > 0)
        {
            foreach (var mod in list.OrderBy(m => m.ModifierDataSO.Priority))
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


    public void AddModifierById(string id, NetworkObject source = null)
    {
        if (modifierDatabaseSO == null)
        {
            Debug.Log("Database null");
        }
        var modSO = modifierDatabaseSO.GetModifierDataSOByID(id);
        var iMod = modSO.CreateInstance(source);
        Debug.Log(modSO.ModID.Value);
        Debug.Log(iMod.GetType().Name);
        AddModifier(iMod);
    }

    public void RemoveModifierByID(string id)
    {
        var modSO = modifierDatabaseSO.GetModifierDataSOByID(id);
        if(_modifiers.TryGetValue(modSO.TargetStat, out var list))
        {
            var iMOd = list.FirstOrDefault(x => x.ModifierDataSO.ModID == id);
            if (iMOd != null && iMOd != default)
            {
                RemoveModifier(iMOd);
            }
        }
    }

    public void AddModifier(IModifier modifier)
    {
        if (modifier == null)
        {
            Debug.LogError("Modifier null");
        }
        if (_modifiers == null)
        {
            Debug.LogError("_Modifiers nulls");
        }
        if (modifier.ModifierDataSO == null) 
        {
            Debug.LogError("Modifier data null");
        }
        if (!_modifiers.ContainsKey(modifier.ModifierDataSO.TargetStat) || !_modifiers.TryGetValue(modifier.ModifierDataSO.TargetStat, out var list))
        {
            Debug.Log("New List modifier");
            list = new List<IModifier>();
            _modifiers[modifier.ModifierDataSO.TargetStat] = list;
        }

        list.Add(modifier);
        MarkDirty(modifier.ModifierDataSO.TargetStat);

        // Nếu là TimedModifier thì theo dõi riêng
        if (modifier is TimeModifier timed)
            _timedModifiers.Add(timed);
    }

    public void RemoveModifier(IModifier modifier)
    {
        if (_modifiers.TryGetValue(modifier.ModifierDataSO.TargetStat, out var list))
        {
            if (list.Remove(modifier))
                MarkDirty(modifier.ModifierDataSO.TargetStat);
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
        Debug.Log("Time modifier: " + _timedModifiers.First().ModifierDataSO.ModID.Value);
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