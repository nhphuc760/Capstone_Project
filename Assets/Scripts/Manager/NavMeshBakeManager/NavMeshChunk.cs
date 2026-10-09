using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshChunk : MonoBehaviour
{
    public NavMeshSurface surface;
    public Vector2Int coord;
    public Bounds bounds;

    /// <summary>
    /// NavMeshLinks that touch this chunk (shared edges with neighbors).
    /// Used to refresh only relevant links after a dirty bake.
    /// </summary>
    //public readonly List<NavMeshLink> neighborLinks = new();

    public bool VolumeContain(Vector3 position)
    {
        return bounds.Contains(position);
    }

    public AsyncOperation UpdateNavMesh()
    {
        return surface.UpdateNavMesh(surface.navMeshData);
    }
}
