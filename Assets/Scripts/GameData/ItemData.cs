using UnityEngine;

/*
Check xem object data da day du cac thanh phan can thiet chua, neu chua thi tra ve gia tri mac dinh
Lam cau noi cho object data de lay cac thong tin cu the nhu itemID, objectName, icon, objectType, stackable, maxStack, objectPoints, weight
*/

[CreateAssetMenu(fileName = "ItemData", menuName = "Game Data/Objects Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Object Reference")]
    public ObjectData objectData;

    public int ItemID => objectData != null ? objectData.itemID : 0;
    public string ItemName => objectData != null ? objectData.objectName : "";
    public Sprite Icon => objectData != null ? objectData.icon : null;
    public ObjectType ObjectType => objectData != null
        ? objectData.objectType
        : ObjectType.None;

    public bool Stackable => objectData != null && objectData.stackable;
    public int MaxStack => objectData != null ? objectData.maxStack : 1;

    public int Points => objectData != null ? objectData.objectPoints : 0;
    //public float Weight => objectData != null ? objectData.weight : 0f;
}