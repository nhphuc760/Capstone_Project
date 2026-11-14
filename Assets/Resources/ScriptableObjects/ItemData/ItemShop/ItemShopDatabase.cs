using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ItemShopDatabase", menuName = "Shop/ItemShopDatabase")]
public class ItemShopDatabase : ScriptableObject
{
    public List<ItemShop> itemShops = new();
}


[Serializable]
public class ItemShop
{
    public ItemSO itemSO;
    public NetworkResourcePlayer price;
}
