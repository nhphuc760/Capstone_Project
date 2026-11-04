using UnityEngine;

public interface IHarvestable : IInteractable
{
    ToolType RequiredTool { get; }
    ResourceType ResourceType { get; }
    bool IsDepleted { get; }
    int Extract();
}
