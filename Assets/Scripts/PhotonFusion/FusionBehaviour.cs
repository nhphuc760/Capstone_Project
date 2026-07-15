using Fusion;
using UnityEngine;

public abstract class FusionBehaviour : NetworkBehaviour
{
    protected ItemDatabase Database => FusionManager.Instance.ItemDatabase;
}