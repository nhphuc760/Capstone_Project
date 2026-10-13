using Fusion;
using UnityEngine;

public static class ModifierIDGenerator
{
    private static int _nextId = 1;

    /// <summary>
    /// Lấy ID mới. Chỉ gọi từ Host / StateAuthority.
    /// </summary>
    public static int Next(NetworkRunner runner)
    {
        if (runner == null || !runner.IsServer)
        {
            Debug.LogWarning("[GameIdGenerator] Chỉ Host được tạo ID.");
            return 0;
        }

        // Tránh overflow (hiếm khi xảy ra)
        if (_nextId == int.MaxValue)
            _nextId = 1;

        return _nextId++;
    }

    /// <summary>
    /// Reset khi bắt đầu ván mới (gọi trên Host).
    /// </summary>
    public static void Reset()
    {
        _nextId = 1;
    }
}
