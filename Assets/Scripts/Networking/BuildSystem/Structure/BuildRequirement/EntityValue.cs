using System;
using Fusion;
using UnityEngine;

[Serializable]
public class EntityValue
{
    public ScriptableObject dataSO;
    public EntityValueType Type;
    public Sprite Icon;
    public int Value;
}

public enum EntityValueType
{
    Wood,
    Stone,
    IronOre,
    CopperOre,
    Gold,
    Electric,
    Item
}
