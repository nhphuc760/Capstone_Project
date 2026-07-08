using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    [Header("Area")]
    public SpawnAreaType areaType;

    [Header("Spawn Points")]
    public List<SpawnPoints> spawnPoints = new();

    [Header("Runtime")]
    public bool isActivated;

    //Onvalidate dùng để tự động cập nhật danh sách spawnPoints khi có sự thay đổi trong Inspector, giúp tránh việc phải thêm thủ công các spawn points vào danh sách.
    //Đảm bảo spawnPoints là lớp con của SpawnArea, vì vậy nó sẽ tìm kiếm tất cả các SpawnPoints trong các đối tượng con của SpawnArea.
    private void OnValidate()
    {
        spawnPoints.Clear();

        foreach (SpawnPoints point in GetComponentsInChildren<SpawnPoints>())
        {
            spawnPoints.Add(point);
        }
    }

    public void ActivateArea()
    {
        if (isActivated)
            return;

        isActivated = true;

        Debug.Log($"{areaType} Activated");

        LevelManager.Instance.SpawnObjects(this);
    }
}