using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using UnityEngine;

public class AuctionManager : NetworkBehaviour
{
    [SerializeField] AuctionItemDatabase auctionDatabaseItem;

    [Tooltip("Tỉ lệ hoàn trả khi đấu giá thất bại")]
    [SerializeField] float refundRate = 80f;
    [SerializeField] int woodPerMoney = 1;
    [SerializeField] int stonePerMoney = 2;
    [SerializeField] int goldPerMoney = 5;
    public float RefundRate => refundRate;  

    Dictionary<PlayerRef, BiderInfo> biders = new();
    [Networked, OnChangedRender(nameof(OnHighestBiderChanged))]
    BiderInfo highestBider { get; set; }

    ItemAuctionInfo CurItemSession;
    
    List<ItemAuctionInfo> unAvailableItem = new();






    EventBinding<ReliableReceiveData> auctionToken;


    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            auctionToken = new EventBinding<ReliableReceiveData>();
            EventBus<ReliableReceiveData>.Register(auctionToken);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {            
            EventBus<ReliableReceiveData>.Deregister(auctionToken);
        }
    }


    public int ConvertToMoney(int wood, int stone, int gold)
    {
        int money = 0;
        money += wood * woodPerMoney;
        money += stone * stonePerMoney;
        money += gold * goldPerMoney;
        return money;
    }


    void OnHighestBiderChanged()
    {
        //UpdateUI
    }

    void StartAuctionSession()
    {
        CurItemSession = auctionDatabaseItem.auctionDatabase.Except(unAvailableItem).RandomElement();
        //UpdateUI
    }

    void EndAuctionSession()
    {
        if (!HasStateAuthority) return;
        Runner.GetPlayerObject(highestBider.Player).GetBehaviour<NetworkInventory>().TryAddItem(CurItemSession.ItemSO.ItemId, CurItemSession.Amount);


        foreach (var i in biders)
        {
            string tokenAuctionResult = "";
            if (i.Key == highestBider.Player)
            {
                tokenAuctionResult = $"Đấu giá thành công. ".ToColor(Color.green) + "Bạn nhận được {CurItemSession.ItemSO.ItemName}.";
            }
            else
            {
                //Hoàn % tiền ở đây
                var refundResource = new NetworkResourcePlayer
                {
                    Wood = Mathf.RoundToInt((float)i.Value.wood * refundRate / 100f),
                    Stone = Mathf.RoundToInt((float)i.Value.stone * refundRate / 100f),
                    GoldOre = Mathf.RoundToInt((float)i.Value.gold * refundRate / 100f)
                };                
                Runner.GetPlayerObject(i.Key).GetBehaviour<NetworkPlayer>().AddResource(refundResource);
                tokenAuctionResult = $"Đấu giá thất bại. ".ToColor(Color.orangeRed) + "Bạn được hoàn {refundRate}% tiền mỗi đơn vị";
            }

            var data = Encoding.UTF8.GetBytes(tokenAuctionResult);
            Runner.SendReliableDataToPlayer(i.Key, ConstReliableKey.AuctionResult, data);
        }
        biders.Clear();
        unAvailableItem.Add(CurItemSession);
        CurItemSession = null;
    }

    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if(data.key == ConstReliableKey.AuctionResult)
        {
            string s = Encoding.UTF8.GetString(data.data);
            NotificationManager.Ins.ShowNotification(s);
        }
    }

    //RPC.

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestTrading(BiderInfo info)
    {

        NetworkPlayer bider = Runner.GetPlayerObject(info.Player).GetBehaviour<NetworkPlayer>();
        if (TradingValid(bider.resourcePlayer, info))
        {
            bider.ComsumpResource(new NetworkResourcePlayer
            {
                Wood = info.wood,
                Stone = info.stone,
                GoldOre = info.gold,
            });

            if (biders.ContainsKey(info.Player))
            {

            }
        }
        else
        {

        }

    }

    public static bool TradingValid(NetworkResourcePlayer resouce, BiderInfo info)
    {
        if (resouce.Wood < info.wood) return false;
        if (resouce.Stone < info.stone) return false;
        if (resouce.GoldOre < info.gold) return false;
        return true;
    }


}


public class ItemAuctionInfo
{
    public ItemSO ItemSO;
    public int Amount;
    [Tooltip("Giá trị khởi điểm")]
    public int StartingValue;
}


public struct BiderInfo: INetworkStruct
{
    public PlayerRef Player;
    public int wood;
    public int stone;
    public int gold;    
}

