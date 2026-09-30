using Fusion;
using UnityEngine;

public interface ITakedamageable
{
    void TakeDamage(int amount, NetworkObject attacker);
}
