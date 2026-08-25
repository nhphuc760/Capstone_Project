using UnityEngine;
using Fusion;

public enum ItemType
{
    None,
    Weapon,
    Consumable
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Online Inventory/ItemSO")]
public class ItemSO : ScriptableObject
{
    [Header("Item Identity")]
    [SerializeField] private int itemId;
    [SerializeField] private string itemName;
    [SerializeField] private ItemType itemType;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject objectPrefab;

    [Header("Inventory")]
    [SerializeField] private bool stackable = true;
    [SerializeField] private int maxStack = 1;

    public int ItemId => itemId;
    public string ItemName => itemName;
    public ItemType ItemType => itemType;

    public Sprite Icon => icon;
    public GameObject ObjectPrefab => objectPrefab;

    public bool Stackable => stackable;
    public int MaxStack => maxStack;   

    private void OnValidate() //dùng để kiểm tra và điều chỉnh giá trị maxStack khi stackable thay đổi
    {
        if (maxStack < 1)
            maxStack = 1;

        if (!stackable)
            maxStack = 1;
    }
}
