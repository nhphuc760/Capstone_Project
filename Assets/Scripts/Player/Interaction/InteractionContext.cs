using UnityEngine;

public class InteractionContext
{
    public InteractPhase Phase;
    public float HoldTime;
    public NetworkPlayer Interactor;
}

public enum InteractPhase
{
    Press,
    Hold,
    Release,
    Cancel
}
