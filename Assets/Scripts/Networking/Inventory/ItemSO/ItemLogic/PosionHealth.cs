using System;
using UnityEngine;

[Serializable]
public class PosionHealth : ItemLogic
{
    [SerializeField] float HealthPosion;
    public override void Use(NetworkPlayer player)
    {
        player.Health.Restore(HealthPosion);
    }   
}
