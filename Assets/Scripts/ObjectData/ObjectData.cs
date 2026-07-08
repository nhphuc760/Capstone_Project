using UnityEngine;

public enum ObjectType
{
    None,
    Valuable,
    QuestItem,
    Consumable
}

[CreateAssetMenu(fileName = "ObjectData", menuName = "Objects Data/Object Data")]
public class ObjectData : ScriptableObject
{
    [Header("Objects Information")]
    public string objectName;
    public Sprite icon;
    public ObjectType objectType;
    public GameObject objectPrefab;

    [Header("Object Points")]
    public int objectPoints;

    [Header("Physics")]
    [Min(0)]
    public float weight;

    [Header("Gameplay")]
    public bool isQuestItem;
    public bool isRare;
    public int difficultyMultiplier = 1;
}