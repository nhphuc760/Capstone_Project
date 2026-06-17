using UnityEngine;
using System.Collections.Generic;
public class InputModifierStack
{
   HashSet<IInputModifier> modifiers = new HashSet<IInputModifier>();

    public bool AddModifier(IInputModifier modifier) => modifiers.Add(modifier);
    public bool RemoveModifier(IInputModifier modifier) => modifiers.Remove(modifier);
    public InputMapContext Process(InputMapContext input)
    {
        foreach (var modifier in modifiers)
        {
            input = modifier.Modify(input);
        }
        return input;
    }
}
