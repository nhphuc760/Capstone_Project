using System.Collections.Generic;
using UnityEngine;

public class GridNetworkSolver
{
    // Cấu trúc lưu trữ một cạnh nối giữa 2 chỉ số điểm
    public struct Edge
    {
        public int u;
        public int v;
        public Edge(int u, int v) { this.u = u; this.v = v; }
    }

    // Hàm kiểm tra xem hai đoạn thẳng (p1-q1) và (p2-q2) có cắt chéo nhau không
    public static bool AreEdgesIntersecting(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Nếu chung đỉnh thì không tính là cắt chéo
        if (p1 == p2 || p1 == q2 || q1 == p2 || q1 == q2) return false;

        float d1 = Direction(p2, q2, p1);
        float d2 = Direction(p2, q2, q1);
        float d3 = Direction(p1, q1, p2);
        float d4 = Direction(p1, q1, q2);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    private static float Direction(Vector2 pi, Vector2 pj, Vector2 pk)
    {
        return (pk.x - pi.x) * (pj.y - pi.y) - (pj.x - pi.x) * (pk.y - pi.y);
    }
}