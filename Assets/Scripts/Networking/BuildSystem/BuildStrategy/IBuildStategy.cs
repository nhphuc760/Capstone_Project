using Fusion;
using UnityEngine;

public  interface IBuildStategy
{
    
    bool CanBuild(NetworkRunner runner,PlayerRef playerRef , Vector3Int cell);
    void Build(NetworkRunner runner,PlayerRef playerRef, Vector3Int cell);   
}
