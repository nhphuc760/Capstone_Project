using Fusion;
using UnityEngine;

// BẮT BUỘC dùng 'struct', tuyệt đối không dùng 'class'
public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
}