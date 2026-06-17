using System.Collections.Generic;

public class StatsMediator
{
    readonly Dictionary<StatType, List<StatModifier>> modifiers = new();
    readonly Dictionary<StatType, float> baseValueMap = new();
    readonly Dictionary<StatType, float> cachedValue = new();
    readonly Dictionary<StatType, bool> dirtyFlag = new();


    public StatsMediator(CharacterStats baseValue)
    {
        foreach (var stat in baseValue.stats)
        {
            baseValueMap[stat.type] = stat.value;
            cachedValue[stat.type] = stat.value;
            dirtyFlag[stat.type] = false;
            modifiers[stat.type] = new List<StatModifier>();
        }
    }

    public float GetValue(StatType type)
    {
        if (dirtyFlag[type])
            Recalculate(type);
        return cachedValue[type];
    }

    public float GetBaseValue(StatType type)
    {
        return baseValueMap[type];
    }
    public void AddModifier(StatModifier modifier)
    {
        modifiers[modifier.statType].Add(modifier);
        dirtyFlag[modifier.statType] = true;
    }
    public void RemoveModifier(StatModifier modifier)
    {
        modifiers[modifier.statType].Remove(modifier);
        dirtyFlag[modifier.statType] = true;
    }
    public void Update(float deltaTime)
    {
        foreach (var i in modifiers)
        {
            for (int j = i.Value.Count - 1; j >= 0; j--)
            {
                var modifier = i.Value[j];
                if (modifier.MarkedForRemoval)
                {
                    RemoveModifier(modifier);
                    continue;
                }
                modifier.Update(deltaTime);
                if (modifier.isDirty)
                {
                    modifier.isDirty = false;
                    dirtyFlag[modifier.statType] = true;
                }
            }
            if (dirtyFlag[i.Key])
                Recalculate(i.Key);
        }
    }
    public void Recalculate(StatType type)
    {
        var modifiersType = modifiers[type];
        if (modifiersType == null || modifiersType.Count == 0) return;
        float flat = 0;
        float percentAdd = 0;
        float percentMul = 1;        
        float result = baseValueMap[type];
        foreach (var i in modifiersType)
        {
            switch (i.layer)
            {
                case StatModifier.ModifyLayer.Add:
                    flat += i.Value;
                    break;
                case StatModifier.ModifyLayer.Sub:
                    flat -= i.Value;
                    break;
                case StatModifier.ModifyLayer.MultiplyPercent:
                    percentMul *= (1 + i.Value/100);
                    break;
                case StatModifier.ModifyLayer.AddPercent:
                    percentAdd += i.Value/100;
                    break;
                case StatModifier.ModifyLayer.SubPercent:
                    percentAdd -= i.Value/100;
                    break;
                default:
                    break;
            }
        }
        result += flat;
        result *= 1 + percentAdd;
        result *= percentMul;
        cachedValue[type] = result;
        dirtyFlag[type] = false;
    }
}
