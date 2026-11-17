using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;

public enum ItemType
{
    None,
    Consumable,
    Valuable,
    Tool
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Item/ItemSO")]
public class ItemSO : ScriptableObject
{
    [Header("Item Identity")]
    [SerializeField] private int itemId;
    [SerializeField] private string itemName;
    [SerializeField] private string description;
    public ItemType itemType;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject objectPrefab;

    [Header("Inventory")]
    [SerializeField] private bool stackable = true;
    [SerializeField] private int maxStack = 1;

    [Header("Behaviours")]
    [SerializeReference]
    public List<ItemComponent> Components = new List<ItemComponent>();
    public int ItemId => itemId;
    public string ItemName => itemName;
    public ItemType ItemType => itemType;

    public Sprite Icon => icon;
    public GameObject ObjectPrefab => objectPrefab;

    public bool Stackable => stackable;
    public int MaxStack => maxStack;

    public string Description => description;

    private void OnValidate() //dùng để kiểm tra và điều chỉnh giá trị maxStack khi stackable thay đổi
    {
        if (maxStack < 1)
            maxStack = 1;
        if (!stackable)
            maxStack = 1;
        _cache = null;
        foreach (var c in Components) c?.Validate(this);
    }

    // ---------- Truy vấn ----------

    Dictionary<Type, ItemComponent> _cache;

    public bool TryGet<T>(out T component) where T : ItemComponent
    {
        if (_cache == null) BuildCache();
        if (_cache.TryGetValue(typeof(T), out var c)) { component = (T)c; return true; }

        // hỗ trợ truy vấn theo class cha / abstract
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T t) { _cache[typeof(T)] = t; component = t; return true; }
        }
        component = null;
        return false;
    }

    public T Get<T>() where T : ItemComponent => TryGet<T>(out var c) ? c : null;
    public bool Has<T>() where T : ItemComponent => TryGet<T>(out _);
    void BuildCache()
    {
        _cache = new Dictionary<Type, ItemComponent>();
        foreach (var c in Components)
            if (c != null) _cache[c.GetType()] = c;
    }

    void OnEnable() => _cache = null;   

}
