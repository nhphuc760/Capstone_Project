using Fusion;
using UnityEngine;

public class PlayerPreviewNetwork : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> Name { get; private set; }
    public override void Spawned()
    {
       
    }

    public override void FixedUpdateNetwork()
    {

    }

}

