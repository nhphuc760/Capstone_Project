using System;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;

public static class EnclosedChecker 
{
    public struct BoudingBox
    {
        public int startX;
        public int endX;
        public int startY;
        public int endY;

        public override string ToString()
        {
            return $"StartX: {startX}\tEndX: {endX}\tStartY: {startY}\tEndY: {endY}";
        }

    }


    static readonly Vector3Int[] Dirs8 = new Vector3Int[]
    {
        new Vector3Int(-1, 0, 1), // trên trái
        new Vector3Int(0,  0, 1), // trên
        new Vector3Int(1, 0,  1), // trên phải
        new Vector3Int( -1, 0, 0), // trái
        new Vector3Int( 1,0,  0), // phải
        new Vector3Int(-1, 0, -1), // dưới trái
        new Vector3Int(0, 0, -1), // dưới
        new Vector3Int( 1,  0, -1)  // dưới phải
    };

    static readonly Vector3Int[] Dirs4 = new Vector3Int[]
{
      new Vector3Int(0,  0, 1), // trên
      new Vector3Int(0, 0, -1), // dưới
      new Vector3Int( -1, 0, 0), // trái
      new Vector3Int( 1,0,  0), // phải
};

    public static Vector3Int[] GetNeighborS8Cell(Vector3Int cell, int width, int height)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        foreach (var dir in Dirs8)
        {
            Vector3Int neighbor = cell + dir;
            if (neighbor.x >= 1 && neighbor.x < width - 1 && neighbor.z >= 1 && neighbor.z < height - 1)
            {
                result.Add(neighbor);
            }
        }
        return result.ToArray();
    }




    public static Vector3Int[] GetNeighborS4Cell(Vector3Int cell, int width, int height)
    {

        List<Vector3Int> result = new List<Vector3Int>();
        foreach (var dir in Dirs4)
        {
            Vector3Int neighbor = cell + dir;
            Debug.Log("Neighbor: " + neighbor);
            if (neighbor.x >= 1 && neighbor.x < width - 1 && neighbor.z >= 1 && neighbor.z < height - 1)
            {
                result.Add(neighbor);
            }
        }
        return result.ToArray();
    }




    /// <summary>
    /// Lấy các ô walls được kết nối với nhau bắt đầu từ cell
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="walls">Danh sách các ô walls đã đặt</param>
    /// <param name="width">chiều dài toàn bộ map</param>
    /// <param name="height">chiều rộng toàn bộ map</param>
    /// <returns></returns>
    public static List<Vector3Int> GetConnectedComponent(Vector3Int cell, HashSet<Vector3Int> walls, int width, int height)
    {
        List<Vector3Int> component = new List<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(cell);
        visited.Add(cell);
        while (stack.Count > 0)
        {
            Vector3Int cur = stack.Pop();
            component.Add(cur);
            var neighbors = GetNeighborS4Cell(cur, width, height);
            Debug.Log("Neighbors COunt = " + neighbors.Length);
            if (neighbors.Length != 0)
            {
                foreach (var i in neighbors)
                {
                    Debug.Log($"Neighbor: {i}");
                }
            }
            foreach (var dir in neighbors)
            {
                if (!walls.Contains(dir)) 
                {
                    Debug.Log($"Wall not contains {dir}");
                    continue; 
                }
                if (visited.Contains(dir)) 
                {
                    Debug.Log($"Wall already visited {dir}");
                    continue; 
                }
                visited.Add(dir);
                stack.Push(dir);
                DbgDraw.Cube(dir + Vector3.one * 0.5f, Quaternion.identity, Vector3.one, Color.yellow, 0.2f);
            }
        }
        return component;
    }


    public static bool CheckFromNewCell(Vector3Int cell, HashSet<Vector3Int> walls, out List<Vector3Int> enclosedList, int width = 100, int height = 100)
    {
        HashSet<Vector3Int> wallsWithNew = new HashSet<Vector3Int>(walls);
        wallsWithNew.Add(cell);
        List<Vector3Int> component = GetConnectedComponent(cell, wallsWithNew, width, height);

        if (component.Count == 0){
            Debug.Log("Component = 0");
            enclosedList = null;
            return false; 
        }
        Debug.Log("Component Count = " + component.Count);
        BoudingBox boudingBox = GetBoundingCNC(component, width, height);
        int startX = boudingBox.startX;
        int startY = boudingBox.startY;
        int endX = boudingBox.endX;
        int endY = boudingBox.endY;
        Debug.Log("Bounding Box: " + boudingBox.ToString());
        //flood fill
        HashSet<Vector3Int> reachable = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        for (int x = startX; x <= endX; x++)
        {
            TryEnqueue(new Vector3Int(x, 0, startY), wallsWithNew, reachable, queue, boudingBox);
            TryEnqueue(new Vector3Int(x, 0, endY), wallsWithNew, reachable, queue, boudingBox);
        }
        for (int y = startY + 1; y <= endY - 1; y++)
        {
            TryEnqueue(new Vector3Int(startX, 0, y), wallsWithNew, reachable, queue, boudingBox);
            TryEnqueue(new Vector3Int(endX, 0, y), wallsWithNew, reachable, queue, boudingBox);
        }

        Debug.Log("Reachable Count = " + reachable.Count);
        Debug.Log("queue Count = " + queue.Count);

        // lan tỏa
        while (queue.Count > 0)
        {
            Vector3Int cur = queue.Dequeue();
            foreach (var dir in GetNeighborS8Cell(cur, width, height))
            {
                if (dir.x < startX || dir.x > endX || dir.z < startY || dir.z > endY) continue;
                if (wallsWithNew.Contains(dir) || reachable.Contains(dir)) continue;
                reachable.Add(dir);
                queue.Enqueue(dir);
                DbgDraw.Cube(dir + Vector3.one * 0.5f, Quaternion.identity, Vector3.one, Color.yellow, 0.3f);
            }
        }

        // kiểm tra có ô nào bị cô lập không ?
        enclosedList = new List<Vector3Int>();
        for (int x = startX; x <= endX; x++)
        {
            for (int z = startY; z <= endY; z++)
            {
                Vector3Int j = new Vector3Int(x, 0, z);
                if (!wallsWithNew.Contains(j) && !reachable.Contains(j))
                    enclosedList.Add(j);
                // Có vùng bị cô lập
            }
        }
        Debug.Log("Enclosed Count: " + enclosedList.Count);
        return enclosedList.Count != 0;
    }
    static void TryEnqueue(Vector3Int cell, HashSet<Vector3Int> walls, HashSet<Vector3Int> reachable, Queue<Vector3Int> queue, BoudingBox boudingBox)
    {
        if (cell.x < boudingBox.startX || cell.x > boudingBox.endX || cell.z < boudingBox.startY || cell.z > boudingBox.endY) return;
        if (walls.Contains(cell) || reachable.Contains(cell)) return;
        DbgDraw.Cube(cell + Vector3.one * 0.5f, Quaternion.identity, Vector3.one, Color.green, 1f);
        reachable.Add(cell);
        queue.Enqueue(cell);
    }

    /// <summary>
    /// Lấy bounding box của connected component
    /// </summary>
    /// <param name="Component"></param>
    /// <param name="width">là chiều dài toàn bộ map</param>
    /// <param name="height">là chiều rộng toàn bộ map</param>
    /// <returns></returns>
    static BoudingBox GetBoundingCNC(List<Vector3Int> Component, int width, int height)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var cell in Component)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.z < minY) minY = cell.z;
            if (cell.z > maxY) maxY = cell.z;
        }
        // Mở rộng 1 ô
        int startX = Mathf.Max(0, minX - 1);
        int endX = Mathf.Min(width - 1, maxX + 1);
        int startY = Mathf.Max(0, minY - 1);
        int endY = Mathf.Min(height - 1, maxY + 1);

        return new BoudingBox
        {
            startX = startX,
            startY = startY,
            endX = endX,
            endY = endY
        };
    }

    static bool IsStraightDegree2(Vector3Int cell, HashSet<Vector3Int> walls)
    {
        List<Vector3Int> neighbors = GetNeighborIsWall(cell, walls);

        if (neighbors.Count != 2) return false;

        Vector3Int a = neighbors[0];
        Vector3Int b = neighbors[1];

        // Hai hướng từ cell đến 2 hàng xóm
        Vector3Int dirA = a - cell;
        Vector3Int dirB = b - cell;

        // Cùng trục khi tổng vector = (0,0) (đối xứng qua cell)
        // Ví dụ: trái + phải = (0,0), trên + dưới = (0,0)
        return dirA + dirB == Vector3Int.zero;
    }


    /// <summary>
    /// Lấy cell bậc 2 có 2 node thẳng hàng gần nhất. Cho phép sử dụng filter để lọc kết quả (Ví dụ: không lấy cell của người khác)
    /// </summary>
    /// <param name="start"></param>
    /// <param name="walls"></param>
    /// <returns></returns>

    public static Vector3Int FindNearestStraightDegree2(Vector3Int start, HashSet<Vector3Int> walls, Func<Vector3Int, bool> filter = null)
    {
        if (IsStraightDegree2(start, walls)) return start;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            if (current != start && IsStraightDegree2(current, walls)) return current;
            foreach (var dir in Dirs4)
            {
                Vector3Int next = current + dir;
                if (!walls.Contains(next)) continue;
                if (visited.Contains(next)) continue;

                //Áp dụng filter nếu có
                if (filter != null && !filter(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return start;
    }
    /// <summary>
    /// Lấy các neighbor chứa walls
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="walls"></param>
    /// <returns></returns>

    static List<Vector3Int> GetNeighborIsWall(Vector3Int cell, HashSet<Vector3Int> walls)
    {
        List<Vector3Int> result = new List<Vector3Int>(4);
        foreach (var dir in Dirs4)
        {
            Vector3Int next = cell + dir;
            if (walls.Contains(next))
                result.Add(next);
        }
        return result;
    }

}
