using System;
using UnityEngine;

[Serializable]
public class BuffHealthRegen : ItemLogic
{
    [SerializeField] float duration;
    [SerializeField] float RegenAmount;
    public override void Use(NetworkPlayer player)
    {

    }    
}
