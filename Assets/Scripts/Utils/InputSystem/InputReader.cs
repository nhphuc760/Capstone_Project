using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader
{
    InputAction Move;
    InputAction Look;
    InputAction Jump;
    InputAction Interact;
    InputAction Attack;
    InputMapContext ctx;
    public InputReader(InputActionMap actionMap)
    {
        Move = actionMap.FindAction("Move");
        Look = actionMap.FindAction("Look");
        Jump = actionMap.FindAction("Jump");
        Interact = actionMap.FindAction("Interact");
        Attack = actionMap.FindAction("Attack");        
    }
    public InputMapContext ReadInput()
    {
        ctx.Move = Move.ReadValue<Vector2>();
        ctx.Look = Look.ReadValue<Vector2>();
        ctx.Jump = Jump.WasPressedThisFrame();
        ctx.Interact = Interact.IsPressed();
        ctx.Attack = Attack.WasPressedThisFrame();
        return ctx;
    }
}