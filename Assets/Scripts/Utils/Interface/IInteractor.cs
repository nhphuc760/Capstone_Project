using UnityEngine;

public class InteractionData
{
    public EffectInteract EffectType;
    public float value;
}

public enum EffectInteract 
{ 
    ModifyHealth,
    ModifyCurrency,
    AddBuff,
    AddInventoryItem
}


public interface IInteractor
{
    void ApplyResultInteraction(InteractionData interactionData);
}

