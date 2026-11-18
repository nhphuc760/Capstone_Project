using Fusion;
using UnityEngine;


public enum ButtonType
{
    InteractMain,
    InteractSecondary,
    CancelInteract,
    Jump,
    EquipTool,
    TestAddModify,
}


public struct NetworkInputData : INetworkInput
{
    public NetworkButtons button;
    public Vector3 moveDirection;
    public Vector2 pitchYaw;
     
}