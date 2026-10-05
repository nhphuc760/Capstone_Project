using Fusion;
using UnityEngine;


public enum ButtonType
{
    Jump,
    Interact,
    EquipTool,
    TestTakeDamage,
}


public struct NetworkInputData : INetworkInput
{
    public NetworkButtons button;
    public Vector3 moveDirection;
    public Vector2 pitchYaw;
     
}