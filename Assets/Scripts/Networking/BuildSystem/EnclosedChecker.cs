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

    static readonly Vector3Int[] Dirs8 =
    {
        new Vector3Int(-1, 0, 1),  // trên trái
        new Vector3Int( 0, 0, 1),  // trên
        new Vector3Int( 1, 0, 1),  // trên phải
        new Vector3Int(-1, 0, 0),  // trái
        new Vector3Int( 1, 0, 0),  // phải
        new Vector3Int(-1, 0,-1),  // dưới trái
        new Vector3Int( 0, 0,-1),  // dưới
        new Vector3Int( 1, 0,-1),  // dưới phải
    };

    static readonly Vector3Int[] Dirs4 =
    {
        new Vector3Int( 0, 0, 1),  // trên
        new Vector3Int( 0, 0,-1),  // dưới
        new Vector3Int(-1, 0, 0),  // trái
        new Vector3Int( 1, 0, 0),  // phải
    };

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public static Vector3Int[] GetNeighborS8Cell(Vector3Int cell, int width, int height)
    {
        var result = new List<Vector3Int>(8);
        foreach (var dir in Dirs8)
        {
            Vector3Int n = cell + dir;
            if (n.x >= 1 && n.x < width - 1 && n.z >= 1 && n.z < height - 1)
                result.Add(n);
        }
        return result.ToArray();
    }

    public static Vector3Int[] GetNeighborS4Cell(Vector3Int cell, int width, int height)
    {
        var result = new List<Vector3Int>(4);
        foreach (var dir in Dirs4)
        {
            Vector3Int n = cell + dir;
            if (n.x >= 1 && n.x < width - 1 && n.z >= 1 && n.z < height - 1)
                result.Add(n);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Component tường nối 4 hướng (logic cũ – dùng cho CheckFromNewCell / Door).
    /// </summary>
    public static List<Vector3Int> GetConnectedComponent(Vector3Int cell, HashSet<Vector3Int> walls, int width, int height)
    {
        return GetConnectedComponentInternal(cell, walls, width, height, use8Dir: false);
    }

    /// <summary>
    /// Component tường nối 8 hướng – bắt được cụm tường chỉ chạm chéo nhau.
    /// </summary>
    public static List<Vector3Int> GetConnectedComponent8(Vector3Int cell, HashSet<Vector3Int> walls, int width, int height)
    {
        return GetConnectedComponentInternal(cell, walls, width, height, use8Dir: true);
    }

    static List<Vector3Int> GetConnectedComponentInternal(Vector3Int cell, HashSet<Vector3Int> walls, int width, int height, bool use8Dir)
    {
        var component = new List<Vector3Int>();
        var visited = new HashSet<Vector3Int>();
        var stack = new Stack<Vector3Int>();

        stack.Push(cell);
        visited.Add(cell);

        while (stack.Count > 0)
        {
            Vector3Int cur = stack.Pop();
            component.Add(cur);

            var neighbors = use8Dir
                ? GetNeighborS8Cell(cur, width, height)
                : GetNeighborS4Cell(cur, width, height);

            foreach (var n in neighbors)
            {
                if (!walls.Contains(n) || visited.Contains(n)) continue;
                visited.Add(n);
                stack.Push(n);
                DbgDraw.Cube(n + Vector3.one * 0.5f, Quaternion.identity, Vector3.one, Color.yellow, 0.2f);
            }
        }
        return component;
    }

    static BoudingBox GetBoundingCNC(List<Vector3Int> component, int width, int height)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var c in component)
        {
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.z < minZ) minZ = c.z;
            if (c.z > maxZ) maxZ = c.z;
        }

        return new BoudingBox
        {
            startX = Mathf.Max(0, minX - 1),
            endX = Mathf.Min(width - 1, maxX + 1),
            startY = Mathf.Max(0, minZ - 1),
            endY = Mathf.Min(height - 1, maxZ + 1)
        };
    }

    static HashSet<Vector3Int> FloodFromBorder(HashSet<Vector3Int> walls, BoudingBox box, int width, int height)
    {
        var reachable = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();

        void TryEnqueue(Vector3Int c)
        {
            if (c.x < box.startX || c.x > box.endX || c.z < box.startY || c.z > box.endY) return;
            if (walls.Contains(c) || reachable.Contains(c)) return;
            reachable.Add(c);
            queue.Enqueue(c);
        }

        for (int x = box.startX; x <= box.endX; x++)
        {
            TryEnqueue(new Vector3Int(x, 0, box.startY));
            TryEnqueue(new Vector3Int(x, 0, box.endY));
        }
        for (int z = box.startY + 1; z <= box.endY - 1; z++)
        {
            TryEnqueue(new Vector3Int(box.startX, 0, z));
            TryEnqueue(new Vector3Int(box.endX, 0, z));
        }

        while (queue.Count > 0)
        {
            Vector3Int cur = queue.Dequeue();
            foreach (var n in GetNeighborS8Cell(cur, width, height))
            {
                if (n.x < box.startX || n.x > box.endX || n.z < box.startY || n.z > box.endY) continue;
                if (walls.Contains(n) || reachable.Contains(n)) continue;
                reachable.Add(n);
                queue.Enqueue(n);
            }
        }
        return reachable;
    }

    // -------------------------------------------------------------------------
    // API cũ – kiểm tra vùng khép kín
    // -------------------------------------------------------------------------

    /// <summary>
    /// Kiểm tra sau khi thêm <paramref name="cell"/> có tạo thành vùng khép kín không.
    /// </summary>
    public static bool CheckFromNewCell(Vector3Int cell, HashSet<Vector3Int> walls, out List<Vector3Int> enclosedList, int width = 100, int height = 100)
    {
        return CheckFromNewCell(cell, walls, out enclosedList, out _, width, height);
    }

    /// <summary>
    /// Giống CheckFromNewCell, thêm <paramref name="regionCount"/> = số vùng enclosed rời nhau (4-connected).
    /// Hình số 8 đóng kín → regionCount = 2.
    /// </summary>
    public static bool CheckFromNewCell(Vector3Int cell, HashSet<Vector3Int> walls, out List<Vector3Int> enclosedList, out int regionCount, int width = 100, int height = 100)
    {
        regionCount = 0;
        var wallsWithNew = new HashSet<Vector3Int>(walls) { cell };

        List<Vector3Int> component = GetConnectedComponent(cell, wallsWithNew, width, height);
        if (component.Count == 0)
        {
            enclosedList = null;
            return false;
        }

        BoudingBox box = GetBoundingCNC(component, width, height);
        HashSet<Vector3Int> reachable = FloodFromBorder(wallsWithNew, box, width, height);

        enclosedList = new List<Vector3Int>();
        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var j = new Vector3Int(x, 0, z);
                if (!wallsWithNew.Contains(j) && !reachable.Contains(j))
                    enclosedList.Add(j);
            }

        if (enclosedList.Count == 0) return false;

        regionCount = CountEnclosedRegions(enclosedList);
        return true;
    }

    /// <summary>
    /// Đếm số vùng enclosed rời nhau (các ô enclosed nối 4 hướng = 1 region).
    /// </summary>
    public static int CountEnclosedRegions(List<Vector3Int> enclosedCells)
    {
        if (enclosedCells == null || enclosedCells.Count == 0) return 0;

        var set = new HashSet<Vector3Int>(enclosedCells);
        var visited = new HashSet<Vector3Int>();
        int regions = 0;

        foreach (var start in enclosedCells)
        {
            if (visited.Contains(start)) continue;
            regions++;

            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector3Int cur = queue.Dequeue();
                foreach (var d in Dirs4)
                {
                    Vector3Int n = cur + d;
                    if (!set.Contains(n) || visited.Contains(n)) continue;
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }
        return regions;
    }

    /// <summary>
    /// Kiểm tra tập walls hiện tại (không thêm cell mới) còn khép kín không.
    /// Dùng khi destroy wall.
    /// </summary>
    public static bool CheckExisting(HashSet<Vector3Int> walls, out List<Vector3Int> enclosedList, int width = 100, int height = 100)
    {
        enclosedList = new List<Vector3Int>();
        if (walls == null || walls.Count == 0) return false;

        Vector3Int any = walls.First();
        List<Vector3Int> component = GetConnectedComponent(any, walls, width, height);
        if (component.Count == 0) return false;

        BoudingBox box = GetBoundingCNC(component, width, height);
        HashSet<Vector3Int> reachable = FloodFromBorder(walls, box, width, height);

        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var j = new Vector3Int(x, 0, z);
                if (!walls.Contains(j) && !reachable.Contains(j))
                    enclosedList.Add(j);
            }
        return enclosedList.Count > 0;
    }

    // -------------------------------------------------------------------------
    // API mới – lấy các wall cạnh đường thoát
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chạy một lượt, tìm các ô wall nằm cạnh đường thoát / kết cấu không bền.
    /// Cover 2 case:
    /// 1. Gần khép kín theo 4 hướng (thiếu 1–2 ô) → seal được vùng enclosed.
    /// 2. Tường xếp chéo (8 hướng) bao quanh 1 ô trống (dạng + / kim cương) → lỗ không bền.
    /// </summary>
    public static List<Vector3Int> GetEscapeWalls(Vector3Int cell, HashSet<Vector3Int> walls, int width = 100, int height = 100)
    {
        var wallsWithNew = new HashSet<Vector3Int>(walls) { cell };

        // Dùng 8-connected để bắt cụm tường chỉ chạm chéo
        List<Vector3Int> component = GetConnectedComponent8(cell, wallsWithNew, width, height);
        if (component.Count < 4) return new List<Vector3Int>();

        BoudingBox box = GetBoundingCNC(component, width, height);
        HashSet<Vector3Int> reachableOutside = FloodFromBorder(wallsWithNew, box, width, height);
        var componentSet = new HashSet<Vector3Int>(component);

        // Đã FullyEnclosed (có ô cô lập thật) → không phải “gần kín”
        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var j = new Vector3Int(x, 0, z);
                if (!wallsWithNew.Contains(j) && !reachableOutside.Contains(j))
                    return new List<Vector3Int>();
            }

        // ----- Path A: gap seal được vùng enclosed (hình chữ nhật thiếu góc/cạnh) -----
        var sealResult = FindSealableEscapeWalls(wallsWithNew, componentSet, box, width, height, reachableOutside);
        if (sealResult.Count > 0) return sealResult;

        // ----- Path B: lỗ bị bao bởi tường chéo / dạng + (không seal được bằng flood 8) -----
        var diagonalResult = FindDiagonalHoleEscapeWalls(wallsWithNew, componentSet, box, reachableOutside);
        if (diagonalResult.Count > 0) return diagonalResult;

        return new List<Vector3Int>();
    }

    /// <summary>
    /// Path A – gap mà nếu lấp vào sẽ tạo vùng kín thật.
    /// </summary>
    static List<Vector3Int> FindSealableEscapeWalls(
        HashSet<Vector3Int> walls, HashSet<Vector3Int> componentSet, BoudingBox box,
        int width, int height, HashSet<Vector3Int> reachableOutside)
    {
        var gapCandidates = new List<(Vector3Int cell, int wallCount, int borderDist)>();

        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var c = new Vector3Int(x, 0, z);
                if (walls.Contains(c)) continue;
                if (!reachableOutside.Contains(c)) continue;

                int wallCount4 = 0;
                foreach (var d in Dirs4)
                    if (componentSet.Contains(c + d)) wallCount4++;

                // Cho phép cả ô chỉ có 1 neighbor 4-dir nhưng ≥ 2 neighbor 8-dir (góc chéo)
                int wallCount8 = 0;
                foreach (var d in Dirs8)
                    if (componentSet.Contains(c + d)) wallCount8++;

                if (wallCount4 < 2 && wallCount8 < 2) continue;

                int distBorder = Mathf.Min(
                    c.x - box.startX, box.endX - c.x,
                    c.z - box.startY, box.endY - c.z);

                gapCandidates.Add((c, Mathf.Max(wallCount4, wallCount8), distBorder));
            }

        gapCandidates.Sort((a, b) =>
        {
            int cmp = b.wallCount.CompareTo(a.wallCount);
            return cmp != 0 ? cmp : a.borderDist.CompareTo(b.borderDist);
        });

        Vector3Int bestGap = default;
        int bestEnclosed = 0;

        foreach (var (gap, _, _) in gapCandidates)
        {
            int enclosed = CountEnclosedIfFill(walls, box, width, height, gap);
            if (enclosed > bestEnclosed)
            {
                bestEnclosed = enclosed;
                bestGap = gap;
            }
        }

        if (bestEnclosed > 0)
        {
            var escape = GetWallsBesideGap(bestGap, walls, componentSet);
            if (escape.Count > 0)
            {
                Debug.Log($"[GetEscapeWalls/Seal] Gap={bestGap} Enclosed={bestEnclosed} Walls={string.Join(",", escape)}");
                return escape;
            }
        }

        // Cặp 2 gap
        if (gapCandidates.Count <= 8)
        {
            int bestPair = 0;
            Vector3Int bestA = default, bestB = default;
            for (int i = 0; i < gapCandidates.Count; i++)
                for (int j = i + 1; j < gapCandidates.Count; j++)
                {
                    int enclosed = CountEnclosedIfFill(walls, box, width, height,
                        gapCandidates[i].cell, gapCandidates[j].cell);
                    if (enclosed > bestPair)
                    {
                        bestPair = enclosed;
                        bestA = gapCandidates[i].cell;
                        bestB = gapCandidates[j].cell;
                    }
                }
            if (bestPair > 0)
            {
                var combined = GetWallsBesideGap(bestA, walls, componentSet)
                    .Union(GetWallsBesideGap(bestB, walls, componentSet)).ToList();
                Debug.Log($"[GetEscapeWalls/SealPair] Gaps={bestA},{bestB} Enclosed={bestPair}");
                return combined;
            }
        }

        return new List<Vector3Int>();
    }

    /// <summary>
    /// Path B – lỗ trống bị bao quanh bởi tường (kể cả chỉ chạm chéo).
    /// Ví dụ:
    ///   W . W          . W .
    ///   . * .    hoặc  W * W
    ///   W . W          . W .
    /// Ô * có nhiều wall 8-hướng bao quanh nhưng flood 8 vẫn lọt → không seal được,
    /// nhưng vẫn là kết cấu không bền.
    /// </summary>
    static List<Vector3Int> FindDiagonalHoleEscapeWalls(
        HashSet<Vector3Int> walls, HashSet<Vector3Int> componentSet,
        BoudingBox box, HashSet<Vector3Int> reachableOutside)
    {
        Vector3Int bestHole = default;
        int bestScore = 0; // số wall 8-dir bao quanh
        List<Vector3Int> bestNeighbors = null;

        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var c = new Vector3Int(x, 0, z);
                if (walls.Contains(c)) continue;
                // Lỗ phải đi vào được từ ngoài (chưa kín thật)
                if (!reachableOutside.Contains(c)) continue;

                var neighbors = new List<Vector3Int>();
                foreach (var d in Dirs8)
                {
                    Vector3Int n = c + d;
                    if (componentSet.Contains(n))
                        neighbors.Add(n);
                }

                // Cần ít nhất 3 wall bao quanh (chéo hoặc thẳng) mới coi là “lồng”
                if (neighbors.Count < 3) continue;

                // Ưu tiên ô bị bao nhiều nhất
                if (neighbors.Count > bestScore)
                {
                    bestScore = neighbors.Count;
                    bestHole = c;
                    bestNeighbors = neighbors;
                }
            }

        if (bestNeighbors == null || bestNeighbors.Count == 0)
            return new List<Vector3Int>();

        // Lấy tối đa 4 wall gần lỗ nhất (đủ để đánh dấu điểm yếu)
        var escape = bestNeighbors
            .OrderBy(w => Mathf.Abs(w.x - bestHole.x) + Mathf.Abs(w.z - bestHole.z))
            .Take(4)
            .ToList();

        DrawEscapeWalls(escape);
        Debug.Log($"[GetEscapeWalls/DiagonalHole] Hole={bestHole} Score={bestScore} Walls={string.Join(",", escape)}");
        return escape;
    }

    /// <summary>
    /// Lấy wall cạnh 1 gap (ưu tiên đúng 2 ô tạo lối thoát).
    /// </summary>
    static List<Vector3Int> GetWallsBesideGap(Vector3Int gap, HashSet<Vector3Int> walls, HashSet<Vector3Int> componentSet)
    {
        var result = new List<Vector3Int>();

        // 4 hướng trước
        foreach (var dir in Dirs4)
        {
            Vector3Int n = gap + dir;
            if (walls.Contains(n) && componentSet.Contains(n))
                result.Add(n);
        }

        if (result.Count == 2)
        {
            DrawEscapeWalls(result);
            return result;
        }

        // Bổ sung diagonal nếu thiếu (trường hợp góc hở diagonal)
        if (result.Count < 2)
        {
            foreach (var dir in Dirs8)
            {
                if (Mathf.Abs(dir.x) + Mathf.Abs(dir.z) != 2) continue;
                Vector3Int n = gap + dir;
                if (walls.Contains(n) && componentSet.Contains(n) && !result.Contains(n))
                {
                    result.Add(n);
                    if (result.Count >= 2) break;
                }
            }
        }

        // Nhiều hơn 2 → giữ 2 gần gap nhất
        if (result.Count > 2)
        {
            result = result
                .OrderBy(w => Mathf.Abs(w.x - gap.x) + Mathf.Abs(w.z - gap.z))
                .Take(2)
                .ToList();
        }

        DrawEscapeWalls(result);
        return result;
    }

    /// <summary>
    /// Đếm số ô sẽ bị cô lập nếu lấp các gap. Trả về 0 nếu không đóng được vòng.
    /// </summary>
    static int CountEnclosedIfFill(HashSet<Vector3Int> walls, BoudingBox box, int width, int height, params Vector3Int[] gaps)
    {
        var temp = new HashSet<Vector3Int>(walls);
        foreach (var g in gaps) temp.Add(g);

        HashSet<Vector3Int> reachable = FloodFromBorder(temp, box, width, height);

        int count = 0;
        for (int x = box.startX; x <= box.endX; x++)
            for (int z = box.startY; z <= box.endY; z++)
            {
                var j = new Vector3Int(x, 0, z);
                if (!temp.Contains(j) && !reachable.Contains(j))
                    count++;
            }
        return count;
    }

    static void DrawEscapeWalls(List<Vector3Int> walls)
    {
        foreach (var w in walls)
            DbgDraw.Cube(w + Vector3.one * 0.5f, Quaternion.identity, Vector3.one, Color.red, 2f);
    }

    // -------------------------------------------------------------------------
    // Door helper
    // -------------------------------------------------------------------------

    static bool IsStraightDegree2(Vector3Int cell, HashSet<Vector3Int> walls)
    {
        var neighbors = GetNeighborIsWall(cell, walls);
        if (neighbors.Count != 2) return false;
        return (neighbors[0] - cell) + (neighbors[1] - cell) == Vector3Int.zero;
    }

    public static Vector3Int FindNearestStraightDegree2(Vector3Int start, HashSet<Vector3Int> walls, Func<Vector3Int, bool> filter = null)
    {
        if (IsStraightDegree2(start, walls)) return start;

        var queue = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector3Int cur = queue.Dequeue();
            if (cur != start && IsStraightDegree2(cur, walls)) return cur;

            foreach (var dir in Dirs4)
            {
                Vector3Int next = cur + dir;
                if (!walls.Contains(next) || visited.Contains(next)) continue;
                if (filter != null && !filter(next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return start;
    }

    /// <summary>
    /// Lấy ô nằm bên ngoài (không enclosed) đối diện với ô door.
    /// Giả định: door nằm trên đoạn tường thẳng (degree 2), 2 bên dọc tường là wall,
    /// 2 hướng vuông góc còn lại: 1 hướng vào enclosed, 1 hướng ra ngoài.
    /// </summary>
    /// <param name="doorCell">Vị trí ô door</param>
    /// <param name="walls">Tập hợp tất cả wall + door (hoặc chỉ wall cũng được)</param>
    /// <param name="enclosedCells">Danh sách ô nằm trong vùng enclosed (đã có từ CheckFromNewCell)</param>
    /// <returns>Ô bên ngoài đối diện door. Nếu không tìm được thì trả về doorCell.</returns>
    public static Vector3Int GetOutsideCellOppositeDoor(Vector3Int doorCell,HashSet<Vector3Int> walls,
        ICollection<Vector3Int> enclosedCells)
    {
        
        // 1. Tìm 2 neighbor là wall (phải là 2 hướng đối nhau)
        var wallNeighbors = GetNeighborIsWall(doorCell, walls);
        if (wallNeighbors.Count != 2)
            return doorCell; // không phải đoạn thẳng degree 2

        // Kiểm tra thật sự đối nhau
        Vector3Int dir1 = wallNeighbors[0] - doorCell;
        Vector3Int dir2 = wallNeighbors[1] - doorCell;
        if (dir1 + dir2 != Vector3Int.zero)
            return doorCell;

        // 2. Hai hướng vuông góc với tường
        Vector3Int perpA, perpB;
        if (dir1.x != 0) // tường nằm ngang (trái-phải) → vuông góc là trên-dưới
        {
            perpA = new Vector3Int(0, 0, 1);
            perpB = new Vector3Int(0, 0, -1);
        }
        else // tường nằm dọc (trên-dưới) → vuông góc là trái-phải
        {
            perpA = new Vector3Int(1, 0, 0);
            perpB = new Vector3Int(-1, 0, 0);
        }

        Vector3Int candidateA = doorCell + perpA;
        Vector3Int candidateB = doorCell + perpB;

        // 3. Ô nào nằm trong enclosed thì ô còn lại là outside
        bool aEnclosed = enclosedCells != null && enclosedCells.Contains(candidateA);
        bool bEnclosed = enclosedCells != null && enclosedCells.Contains(candidateB);

        if (aEnclosed && !bEnclosed) return candidateB;
        if (bEnclosed && !aEnclosed) return candidateA;

        // Fallback: nếu cả hai đều không enclosed (hoặc cả hai đều enclosed – bất thường)
        // thì ưu tiên ô không phải wall
        if (!walls.Contains(candidateA)) return candidateA;
        if (!walls.Contains(candidateB)) return candidateB;

        return doorCell;
    }

    static List<Vector3Int> GetNeighborIsWall(Vector3Int cell, HashSet<Vector3Int> walls)
    {
        var result = new List<Vector3Int>(4);
        foreach (var dir in Dirs4)
        {
            Vector3Int n = cell + dir;
            if (walls.Contains(n)) result.Add(n);
        }
        return result;
    }
}
