using System;
using UnityEngine;

public interface IState
{
    void Enter();
    void Update(float deltaTime);
    void Exit();

}
