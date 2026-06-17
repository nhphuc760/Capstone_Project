using UnityEngine;

public interface ITakedamageable
{
    public bool IsHold{ get; set; }
    void Takedamage(int damage);
}
