using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public NetworkBool isSprinting;
    public float lookDeltaX; 
    public float lookDeltaY;
}