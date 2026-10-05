using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshChunk : MonoBehaviour
{
    public NavMeshSurface surface;
    public Vector2Int coord;
    public Bounds bounds;

    public bool VolumeContain(Vector3 position)
    {
        return bounds.Contains(position);        
    }

    public void UpdateNavMesh()
    {
        surface.UpdateNavMesh(surface.navMeshData);
    }

}
