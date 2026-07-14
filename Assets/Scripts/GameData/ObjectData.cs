using UnityEngine;

public enum ObjectType
{
    None,
    Valuable,
    ItemEffect,
    Consumable
}

[CreateAssetMenu(fileName = "ObjectData", menuName = "Objects Data/Object Data")]
public class ObjectData : ScriptableObject
{
    [Header("Objects Information")]
    public string objectName;
    public Sprite icon;
    public ObjectType objectType;
    public GameObject objectPrefab; // for 3d objects

    [Tooltip("Chỉ dùng khi ObjectType = ItemEffect")]
    public ObjectEffectData effect;

    [Header("Object Points")]
    public int objectPoints;

    [Header("Physics")]
    [Tooltip("Tự động tính ko cần nhập")]
    [Min(0)]
    public float weight;
    private void OnValidate()
    {
        weight = objectPoints / 20f;
    }

    [Header("Gameplay")]
    public bool isRare;
}