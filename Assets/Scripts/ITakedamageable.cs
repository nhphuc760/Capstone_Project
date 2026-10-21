using Fusion;
using UnityEngine;

public interface ITakedamageable
{
    void TakeDamage(float amount, NetworkObject attacker);
}
