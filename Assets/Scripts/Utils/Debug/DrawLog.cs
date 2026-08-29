using UnityEngine;

public static class DrawLog
{
    public static void DrawCube(Vector3 center, Vector3 extents, Color color, float duration = 1f)
    {
        Vector3[] points = new Vector3[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3( extents.x, -extents.y, -extents.z),
            center + new Vector3( extents.x, -extents.y,  extents.z),
            center + new Vector3(-extents.x, -extents.y,  extents.z),
            center + new Vector3(-extents.x,  extents.y, -extents.z),
            center + new Vector3( extents.x,  extents.y, -extents.z),
            center + new Vector3( extents.x,  extents.y,  extents.z),
            center + new Vector3(-extents.x,  extents.y,  extents.z)
        };

        // Đáy
        Debug.DrawLine(points[0], points[1], color, duration);
        Debug.DrawLine(points[1], points[2], color, duration);
        Debug.DrawLine(points[2], points[3], color, duration);
        Debug.DrawLine(points[3], points[0], color, duration);
        // Đỉnh
        Debug.DrawLine(points[4], points[5], color, duration);
        Debug.DrawLine(points[5], points[6], color, duration);
        Debug.DrawLine(points[6], points[7], color, duration);
        Debug.DrawLine(points[7], points[4], color, duration);
        // Cột nối
        Debug.DrawLine(points[0], points[4], color, duration);
        Debug.DrawLine(points[1], points[5], color, duration);
        Debug.DrawLine(points[2], points[6], color, duration);
        Debug.DrawLine(points[3], points[7], color, duration);

    }
    /// <summary>
    /// Vẽ hình tròn theo hướng bất kỳ
    /// </summary>
    /// <param name="center">Tâm hình tròn</param>
    /// <param name="radius">Bán kính</param>
    /// <param name="normal">Hướng mặt phẳng (Vector3.up cho mặt đất, Vector3.forward cho 2D)</param>
    /// <param name="color">Màu sắc</param>
    /// <param name="duration">Thời gian hiển thị (giây)</param>
    /// <param name="segments">Số đoạn thẳng (Càng cao hình tròn càng mượt, mặc định 32)</param>
    public static void DrawCircle(Vector3 center, float radius, Vector3 normal, Color color, float duration = 0f, int segments = 32)
    {
        // Tạo hệ tọa độ vuông góc dựa trên vector pháp tuyến (normal)
        Vector3 right = Vector3.Cross(normal, Vector3.up);
        if (right.sqrMagnitude < 0.001f) // Trường hợp normal song song với Vector3.up
            right = Vector3.Cross(normal, Vector3.right);

        right.Normalize();
        Vector3 forward = Vector3.Cross(right, normal).normalized;

        float angleStep = 360f / segments;
        Vector3 prevPoint = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            // Công thức lượng giác: P = Center + (Right * cos(a) + Forward * sin(a)) * R
            Vector3 nextPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;

            Debug.DrawLine(prevPoint, nextPoint, color, duration);
            prevPoint = nextPoint;
        }
    }
    public static void DrawSphere(Vector3 center, float radius, Color color, float duration = 0f, int segments = 24)
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep * Mathf.Deg2Rad;
            float a2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            float cos1 = Mathf.Cos(a1) * radius;
            float sin1 = Mathf.Sin(a1) * radius;
            float cos2 = Mathf.Cos(a2) * radius;
            float sin2 = Mathf.Sin(a2) * radius;

            // 1. Vòng tròn trên mặt phẳng XY (Góc nhìn chính diện - Front)
            Vector3 pXY1 = center + new Vector3(cos1, sin1, 0);
            Vector3 pXY2 = center + new Vector3(cos2, sin2, 0);
            Debug.DrawLine(pXY1, pXY2, color, duration);

            // 2. Vòng tròn trên mặt phẳng XZ (Mặt phẳng song song với đất - Top)
            Vector3 pXZ1 = center + new Vector3(cos1, 0, sin1);
            Vector3 pXZ2 = center + new Vector3(cos2, 0, sin2);
            Debug.DrawLine(pXZ1, pXZ2, color, duration);

            // 3. Vòng tròn trên mặt phẳng YZ (Góc nhìn ngang - Side)
            Vector3 pYZ1 = center + new Vector3(0, cos1, sin1);
            Vector3 pYZ2 = center + new Vector3(0, cos2, sin2);
            Debug.DrawLine(pYZ1, pYZ2, color, duration);
        }
    }
}
