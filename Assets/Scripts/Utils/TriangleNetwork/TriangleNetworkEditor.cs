#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TriangleNetwork))]
public class TriangleNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TriangleNetwork network = (TriangleNetwork)target;

        GUILayout.Space(15);
        if (GUILayout.Button("BAKE", GUILayout.Height(40)))
        {
            ExecuteUpdate(network);
        }
    }

    private void ExecuteUpdate(TriangleNetwork network)
    {
        VertexPoint[] children = network.GetComponentsInChildren<VertexPoint>();
        if (children == null || children.Length < 2)
        {
            Debug.LogWarning("Cần ít nhất 2 điểm con để tạo lưới!");
            return;
        }

        Undo.RecordObject(network, "Update Network Points");
        network.allPoints.Clear();
        network.allPoints.AddRange(children);

        // Reset dữ liệu hàng xóm của các điểm con
        foreach (var point in network.allPoints)
        {
            if (point != null)
            {
                Undo.RecordObject(point, "Clear Neighbors Data");
                if (point.neighbors == null) point.neighbors = new List<VertexPoint>();
                point.neighbors.Clear();
            }
        }

        int nodeCount = network.allPoints.Count;
        List<GridNetworkSolver.Edge> validEdges = new List<GridNetworkSolver.Edge>();

        // Tạo danh sách tất cả các cặp cạnh có thể nối, sắp xếp theo khoảng cách từ ngắn đến dài
        // Việc ưu tiên cạnh ngắn giúp tạo ra các mắt lưới phụ kín như tổ ong thay vì nối dây điện chằng chịt xa xôi
        List<System.Tuple<float, GridNetworkSolver.Edge>> allPossibleEdges = new List<System.Tuple<float, GridNetworkSolver.Edge>>();

        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                float dist = Vector3.Distance(network.allPoints[i].transform.position, network.allPoints[j].transform.position);
                allPossibleEdges.Add(new System.Tuple<float, GridNetworkSolver.Edge>(dist, new GridNetworkSolver.Edge(i, j)));
            }
        }

        // Sắp xếp tăng dần theo khoảng cách
        allPossibleEdges.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        // Tiến hành duyệt và nối cạnh (Chặn tuyệt đối cắt chéo)
        foreach (var edgeTuple in allPossibleEdges)
        {
            GridNetworkSolver.Edge candidate = edgeTuple.Item2;

            Vector2 p1 = new Vector2(network.allPoints[candidate.u].transform.position.x, network.allPoints[candidate.u].transform.position.z);
            Vector2 q1 = new Vector2(network.allPoints[candidate.v].transform.position.x, network.allPoints[candidate.v].transform.position.z);

            bool hasIntersection = false;

            // Check xem cạnh định nối này có cắt chéo các cạnh đã nối thành công trước đó không
            foreach (var existingEdge in validEdges)
            {
                Vector2 p2 = new Vector2(network.allPoints[existingEdge.u].transform.position.x, network.allPoints[existingEdge.u].transform.position.z);
                Vector2 q2 = new Vector2(network.allPoints[existingEdge.v].transform.position.x, network.allPoints[existingEdge.v].transform.position.z);

                if (GridNetworkSolver.AreEdgesIntersecting(p1, q1, p2, q2))
                {
                    hasIntersection = true;
                    break;
                }
            }

            // Nếu KHÔNG bị cắt chéo -> Chấp nhận cạnh này vào mạng lưới
            if (!hasIntersection)
            {
                validEdges.Add(candidate);
                Connect(network.allPoints[candidate.u], network.allPoints[candidate.v]);
            }
        }

        // Ép lưu dữ liệu cứng vào Unity
        EditorUtility.SetDirty(network);
        foreach (var point in network.allPoints)
        {
            if (point != null)
            {
                SerializedObject so = new SerializedObject(point);
                so.Update();
                EditorUtility.SetDirty(point);
                so.ApplyModifiedProperties();
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(network.gameObject.scene);

        Debug.Log($"<color=green><b>[Thành công]</b></color> Đã tạo xong lưới với {validEdges.Count} cạnh và {nodeCount} đỉnh!");
    }

    private void Connect(VertexPoint p1, VertexPoint p2)
    {
        if (p1 == null || p2 == null || p1 == p2) return;
        if (!p1.neighbors.Contains(p2)) p1.neighbors.Add(p2);
        if (!p2.neighbors.Contains(p1)) p2.neighbors.Add(p1);
    }
}
#endif