using System;
using UnityEngine;

public interface IInteractable
{  
    Transform InteractTransform { get; }
    Action OnCompleted { get; set; }
    InteractionData Data(NetworkPlayer interactor);
    InteractionResult CanInteract(NetworkPlayer interactor);
    void OnInteract(InteractionContext ctx);    
}

[Serializable]
public class InteractionData
{
    public InteractionMode Mode;
    public string Prompt;
    public float HoldTime;
    public InteractionAnimation InteractorAnimation;
    //public List<.>
}

[Serializable]
public class InteractionAnimation
{
    public string stateName;
    [Tooltip("Hz = 1 / time")]
    public float hzState; // Hz = 1 / time
}


public enum InteractionMode
{
    Press,      // Bấm 1 lần → xong ngay
    Hold,         // Giữ đến khi đủ thời gian
    Release    // Giữ để lặp lại liên tục (mining, attacking)
}

[Serializable]
public class InteractionResult
{
    public bool Success;
    public string Message;
    public InteractionResult(bool success, string message = null)
    {
        Success = success;
        Message = message;
    }
    
}