using Fusion;
using UnityEngine;

public interface IStructureActionStategy
{
    StructureActionResult CanUpgrade(NetworkRunner runner, PlayerRef playerRef, StructureBase structure);
    void Upgrade(NetworkRunner runer, PlayerRef playerRef, StructureBase structure);

    StructureActionResult CanMove(NetworkRunner runner, PlayerRef playerRef, StructureBase structure, Vector3Int newCell);
    void Move(NetworkRunner runner, PlayerRef playerRef, StructureBase structure, Vector3Int newCell);

    StructureActionResult CanDestroy(NetworkRunner runner, PlayerRef playerRef, StructureBase structure);
    void Destroy(NetworkRunner runner, PlayerRef playerRef, StructureBase structure); 
}



public struct StructureActionResult
{
    public bool OK;
    public string Message;
}
