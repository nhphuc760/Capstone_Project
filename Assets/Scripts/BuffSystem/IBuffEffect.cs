using UnityEngine;


public interface IBuffEffect
{
    float Value { get; }
}

public class BuffHealthEffect : IBuffEffect
{
    int amount;
    public BuffHealthEffect(int amount)
    {
        this.amount = amount;
    }

    public float Value => amount;
}

public class BuffStaminaEffect : IBuffEffect
{
    public float Value => throw new System.NotImplementedException();
}
