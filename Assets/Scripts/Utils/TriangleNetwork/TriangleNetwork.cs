using System.Collections.Generic;
using UnityEngine;

public class TriangleNetwork : MonoBehaviour
{
    [Header("Cấu hình hiển thị Debug")]
    [Tooltip("Tích chọn để LUÔN LUÔN hiện lưới. Bỏ tích để CHỈ HIỆN khi chọn đối tượng này.")]
    public bool alwaysShowGizmos = false;
    public Color gridColor = Color.cyan;
    public Color pointColor = Color.yellow;
    // Danh sách lưu trữ để vẽ Debug
    public List<VertexPoint> allPoints = new List<VertexPoint>();

    // Chỉ giữ lại hàm vẽ Debug khi được chọn
    private void OnDrawGizmosSelected()
    {
        if (!alwaysShowGizmos)
        {
            DrawNetworkGizmos();
        }
    }
    private void OnDrawGizmos()
    {
        if (alwaysShowGizmos)
        {
            DrawNetworkGizmos();
        }
    }
    void DrawNetworkGizmos()
    {
        if (allPoints == null || allPoints.Count == 0) return;

        foreach (var point in allPoints)
        {
            if (point == null) continue;

            // Vẽ nút điểm bằng khối cầu màu vàng
            Gizmos.color = pointColor;
            Gizmos.DrawSphere(point.transform.position, 0.4f);

            // Vẽ các đường nối mạng lưới màu xanh cyan
            Gizmos.color = gridColor;
            if (point.neighbors == null) continue;

            foreach (var neighbor in point.neighbors)
            {
                if (neighbor != null && allPoints.IndexOf(point) < allPoints.IndexOf(neighbor))
                {
                    Gizmos.DrawLine(point.transform.position, neighbor.transform.position);
                }
            }
        }
    }
}