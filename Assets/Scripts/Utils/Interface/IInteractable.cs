using UnityEngine;

public interface IInteractable
{
    float HoldDuration { get; }
    bool HoldInteract { get; }
    bool IsInteractable { get; }
    void Interact(IInteractor interactor, InteractionPhase phase);
}

public enum InteractionPhase 
{
    Started,
    Performed,
    Canceled,
}
