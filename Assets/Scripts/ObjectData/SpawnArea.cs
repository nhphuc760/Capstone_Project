using System.Collections.Generic;
using UnityEngine;

//Đoạn code này nằm trong gameobject chứa toàn bộ spawn point của 1 khu vực,model và nó sẽ được quản lý bởi LevelManager
/*Level
│
├── Areas
│   │
│   ├── House_01        ← SpawnArea.cs
│   │   ├── Model
│   │   ├── Trigger
│   │   ├── SpawnPoint_01
│   │   ├── SpawnPoint_02
│   │   └── SpawnPoint_03
*/
public class SpawnArea : MonoBehaviour
{
    [Header("Area")]
    public SpawnAreaType areaType;

    [Tooltip("ID của khu vực cùng loại. Ví dụ House_0, House_1...")]
    [Min(0)]
    public int areaId;

    [Header("Spawn Points")]
    public List<SpawnPoints> spawnPoints = new();

    [Header("Runtime")]
    [SerializeField]
    private AreaState currentState = AreaState.Inactive;

    public AreaState CurrentState => currentState;

    /// <summary>
    /// House_0, Church_1...
    /// </summary>
    public string AreaKey => $"{areaType}_{areaId}";

    private void OnValidate()
    {
        spawnPoints.Clear();
        spawnPoints.AddRange(GetComponentsInChildren<SpawnPoints>());
    }

    public void Activate()
    {
        if (currentState != AreaState.Inactive)
            return;

        currentState = AreaState.Active;

        LevelManager.Instance.SpawnObjects(this);
    }

    public void CompleteArea()
    {
        currentState = AreaState.Cleared;
    }

    public void DisableArea()
    {
        currentState = AreaState.Disabled;
    }

    public bool IsCleared()
    {
        return currentState == AreaState.Cleared;
    }
}