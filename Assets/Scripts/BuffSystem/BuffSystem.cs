using System.Collections.Generic;
using UnityEngine;

public class BuffSystem : MonoBehaviour, IBuffReceiver
{
    Queue<IBuffEffect> effects = new Queue<IBuffEffect>();
    public void ReceiveEffect(IBuffEffect effect)
    {
        effects.Enqueue(effect);
    }

    void ApplyEffect()
    {
        foreach (var effect in effects)
        {

        }
    }

}
