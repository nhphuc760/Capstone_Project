using System;
using UnityEngine;

[Serializable]
public class ToolComponent : ItemComponent
{
    [SerializeField]
    public ToolType toolType;
    public float power = 1f;    
}
