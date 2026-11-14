using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AuctionItemDatabase", menuName = "Shop/AuctionItemDatabase")]
public class AuctionItemDatabase : ScriptableObject
{
    public List<ItemAuctionInfo> auctionDatabase = new();

}
