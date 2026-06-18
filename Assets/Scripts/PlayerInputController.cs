using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    PlayerInput playerInput;
    InputReader inputReader;
    InputModifierStack modifierStack = new();
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();      
        inputReader = new InputReader(playerInput.currentActionMap);
    }

    public InputMapContext GetInput()
    {
        InputMapContext ctx = inputReader.ReadInput();
        ctx = modifierStack.Process(ctx);
        return ctx;
    }

}
