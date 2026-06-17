using UnityEngine;

public interface IInputModifier 
{
    //int priority { get; }
    InputMapContext Modify(InputMapContext input);
}

public class InvertMoveModifier : IInputModifier
{
    //public int priority => 

    public InputMapContext Modify(InputMapContext input)
    {
        input.Move *= -1;
        return input;
    }
}

public class StuneModifier : IInputModifier
{
    //public int priority => 

    public InputMapContext Modify(InputMapContext input)
    {
        input.Move = Vector2.zero;
        input.Attack = false;
        input.Jump = false;
        return input;
    }
}



