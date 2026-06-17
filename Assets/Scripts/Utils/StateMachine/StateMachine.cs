using UnityEngine;

public class StateMachine
{
    IState currentState;
    public void ChangeState(IState newState)
    {
        if(newState != null && currentState != newState)
        {
            currentState.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }
    public void Update()
    {
        if(currentState != null)
        {
            currentState.Update();
        }
    }
}
