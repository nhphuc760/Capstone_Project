using Fusion;
using UnityEngine;

public class PlayerDebug : NetworkBehaviour
{
    public override void Spawned()
    {
        Debug.Log(
            $"[{Runner.GameMode}] PLAYER SPAWNED | " +
            $"Object={Object.name} | " +
            $"InputAuthority={Object.InputAuthority} | " +
            $"HasInputAuthority={Object.HasInputAuthority} | " +
            $"StateAuthority={Object.HasStateAuthority}"
        );
    }
}