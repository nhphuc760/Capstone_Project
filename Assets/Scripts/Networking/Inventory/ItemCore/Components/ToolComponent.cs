using UnityEngine;

public class ToolComponent : ItemComponent
{
    [SerializeField]
    public ToolType toolType;
    public float power = 1f;    
}
