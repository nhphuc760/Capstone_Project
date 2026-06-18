using System;
using System.Collections.Generic;

[Serializable]
public abstract class StatModifier
{
    public StatModifier()
    {
        
    }
    public enum ModifyLayer 
    {
        Add,
        Sub,
        MultiplyPercent,
        AddPercent,
        SubPercent,
    }
    public ModifyLayer layer;
    public bool MarkedForRemoval { get; private set; }
    public bool isDirty;
    public StatType statType;
    float remainingTime;
    public float Value;
    protected StatModifier(float duration)
    {
        if (duration <= 0) return;
        remainingTime = duration;
    }        

    public virtual void Update(float deltaTime)
    {
        //base
        remainingTime -= deltaTime;
        if(remainingTime <= 0)
        {
            MarkedForRemoval = true;
        }
    }    
}