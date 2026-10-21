using UnityEngine;

public class EnemyStateMachine
{
    EnemyBaseState currentState;
    public void ChangeState(EnemyBaseState newState)
    {
        if (newState != null && currentState != newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }
    public void Update(float deltaTime)
    {
        if (currentState != null)
        {
            currentState?.Update(deltaTime);
        }
    }

    public EnemyBaseState CurrentState => currentState;

}
