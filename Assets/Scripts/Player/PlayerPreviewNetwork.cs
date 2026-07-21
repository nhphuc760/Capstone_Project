using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class PlayerPreviewNetwork : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> Name { get; private set; }
    

    public async override void Spawned()
    {
       await  UniTask.WaitUntil(() => LobbyManager.Ins.IsSpawned);
        if (LobbyManager.Ins._playerSlotIndices.TryGet(Object.InputAuthority, out int index))
        {
          var spawnPoint =  LobbyManager.Ins.GetSpawnPoint(index);
          transform.position = spawnPoint.position;
        }
    }

    public override void FixedUpdateNetwork()
    {

    }

}

