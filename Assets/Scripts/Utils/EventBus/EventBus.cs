using System;
using System.Collections.Generic;
using UnityEngine;


public interface IEvent { }


public static class EventBus<T> where T : IEvent
{
   static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    public static void Register(IEventBinding<T> eventBinding) => bindings.Add(eventBinding);
    public static void Deregister(IEventBinding<T> eventBinding) => bindings.Remove(eventBinding);
    public static void Raise(T eventArgs)
    {
        foreach (var binding in bindings)
        {
            binding.OnEvent.Invoke(eventArgs);
            binding.OnEventNoArgs.Invoke();
        }
    }
    public static void Raise()
    {
        foreach (var binding in bindings)
        {
            binding.OnEventNoArgs.Invoke();
        }
    }
}
