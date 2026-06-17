using System.Collections.Generic;
using UnityEngine;

public class VertexPoint : MonoBehaviour
{
    // Danh sách điểm kề sẽ được đối tượng cha tự động điền vào
    public List<VertexPoint> neighbors = new List<VertexPoint>();

    public VertexPoint GetRandomNeighbor()
    {
        if (neighbors == null || neighbors.Count == 0) return null;
        return neighbors[Random.Range(0, neighbors.Count)];
    }
}