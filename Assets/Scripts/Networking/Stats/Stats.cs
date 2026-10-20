using System.Collections.Generic;
using Fusion;
using UnityEngine;


public class Stats : NetworkBehaviour
{
    public StatsBase baseStats;


    [Networked, Capacity(32)]
    NetworkDictionary<int, Modifier> _modifiers => default;
    [Networked, OnChangedRender(nameof(OnDirtyChanged))]
    NetworkBool markDirty {  get; set; }


    Dictionary<StatsType, List<Modifier>> cacheModifiers = new Dictionary<StatsType, List<Modifier>>();



    public float Get(StatsType type)
    {
        float baseValue = GetValueBase(type);
        float value = baseValue;

        if (cacheModifiers.TryGetValue(type, out var list) && list.Count > 0)
        {
            foreach (var mod in list)
            {
                value = mod.Apply(value, baseValue);
            }

        }
        return value;

    }    

    public float GetValueBase(StatsType type)
    {
        return baseStats.stats.TryGetValue(type, out float v) ? v : 0f;
    }

    public void SetStatsBase(StatsBase statsBase)
    {
        this.baseStats = statsBase;
    }
    public void SetValueBase(StatsType type, float value)
    {
        baseStats.stats[type] = value;
    }    

    public void RemoveModifierByID(int id)
    {
        if (!HasStateAuthority) return;
        
        if (_modifiers.Remove(id))
        {
            markDirty = true;
        }      
    }



    public void AddModifier(Modifier modifier)
    {
        if (!HasStateAuthority) return;

        if (modifier.ModID < 1)
        {
            Debug.LogError("Modifier null");
        }
        Debug.Log($"{gameObject.name}: Addmodifier: StatsType = {modifier.StatType.ToString()} Value = {modifier.Value.ToString()} ApplyType = {modifier.ModApplyType.ToString()}");
        _modifiers.Set(modifier.ModID, modifier);
        markDirty = true;              
    }    

    public void ClearAllModifiers()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        _modifiers.Clear();
        markDirty = true;
    }

   

    /// <summary>
    /// Phải gọi mỗi frame (hoặc trong Update của Character)
    /// </summary>
    public void Tick(float deltaTime)
    {
       
        var keysToRemove = new List<int>();
        foreach (var i in _modifiers)
        {
            if (i.Value.ModID > 0 && i.Value.IsTimed)
            {
                if (i.Value.Duration.Expired(Runner))
                {
                    keysToRemove.Add(i.Key);
                }
            }
        }

        foreach (var i in keysToRemove)
        {
            RemoveModifierByID(i);
        }
    }


    void OnDirtyChanged()
    {
        if (markDirty)
        {
            UpdateCached();
        }
    }


    void UpdateCached()
    {
        cacheModifiers.Clear();

        foreach (var i in _modifiers)
        {
            Modifier iMod = i.Value;

            if (!cacheModifiers.TryGetValue(iMod.StatType, out var list))
            {
                list = new List<Modifier>();
                cacheModifiers[iMod.StatType] = list;
            }
            list.Add(iMod);
        }

        // Sort theo Priority
        foreach (var kvp in cacheModifiers)
        {
            if (kvp.Value != null && kvp.Value.Count > 1)
            {
                kvp.Value.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                // hoặc: cacheModifiers[kvp.Key] = kvp.Value.OrderBy(x => x.Priority).ToList();
            }
        }

        markDirty = false;   // ← rất quan trọng, phải reset
    }

}