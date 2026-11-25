using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Fusion;
using Newtonsoft.Json;
using UnityEngine;

public class AuctionManager : NetworkBehaviour
{
    [SerializeField] AuctionItemDatabase auctionDatabaseItem;

    [Tooltip("Tỉ lệ hoàn trả khi đấu giá thất bại")]
    [SerializeField] float refundRate = 80f;
    [SerializeField] int woodPerMoney = 1;
    [SerializeField] int stonePerMoney = 2;
    [SerializeField] int goldPerMoney = 5;
    [SerializeField] ItemDatabase itemDatabase;
    [SerializeField] AuctionSesstionUI AuctionSesstionUI;
    [Networked] TickTimer RemainingTime { get; set; }
    public float RemainingTimeSession => RemainingTime.RemainingTime(Runner).Value;
    CancellationTokenSource endSessionToken;
    public float RefundRate => refundRate;
    AuctionSession curSession;
    Stack<AuctionSession> historySession = new();
    public List<AuctionSession> HistorySession => historySession.ToList();

    EventBinding<ReliableReceiveData> auctionToken;

    public event Action<TradingResult> OnServerRespone_TradingResult;
    public event Action OnStartAuctionSession;
    public event Action OnEndAuctionSession;

    public override void Spawned()
    {
        auctionToken = new EventBinding<ReliableReceiveData>(OnReliableReceiveData);
        EventBus<ReliableReceiveData>.Register(auctionToken);



    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {

        EventBus<ReliableReceiveData>.Deregister(auctionToken);
    }


    private void Update()
    {
        //Test
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (HasStateAuthority)
            {
                StartAuctionSession(60f);
            }
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

    public override void FixedUpdateNetwork()
    {
        if (RemainingTime.Expired(Runner))
        {
            Debug.Log("Kếtt thúc Session Đấu giá");
            endSessionToken.Cancel();
            EndAuctionSession();
            RemainingTime = TickTimer.None;
        }
    }


    public void StartAuctionSession(float seconds)
    {
        if (!HasStateAuthority) return;
        var item = auctionDatabaseItem.auctionDatabase.Except(historySession.Select(x => x.ItemAuctionInfo)).RandomElement();
        curSession = new AuctionSession(this, item);
        curSession.OnHistoryTradingAdded += CurSession_OnHistoryTradingChanged;
        RemainingTime = TickTimer.CreateFromSeconds(Runner, seconds);
        endSessionToken = new CancellationTokenSource();
        AuctionSesstionUI.UpdateSession(curSession, seconds, endSessionToken.Token);
        RPC_UpdateSession(item.ItemSO.ItemId, item.Amount, item.StartingValue, seconds);
        //UpdateUI
    }

    //State Authority only
    private void CurSession_OnHistoryTradingChanged(Trading onAdded)
    {
        // Server gửi dữ liệu cho all.
        string jsonDateTime = JsonConvert.SerializeObject(onAdded.DateTime);
        byte[] data = Encoding.UTF8.GetBytes(jsonDateTime);


        RPC_OnAddedTradingEvents(onAdded.wood, onAdded.stone, onAdded.gold, onAdded.Player, data);
    }

    public void EndAuctionSession()
    {
        if (!HasStateAuthority) return;
        if (curSession == null) return;
        curSession.OnHistoryTradingAdded -= CurSession_OnHistoryTradingChanged;
        curSession.EndSession();       
        if (curSession.Winner.Player == PlayerRef.None) return; // Không ai đấu giá
        //Phần thưởng cho người chiến thắng
        Runner.GetPlayerObject(curSession.Winner.Player)
            .GetBehaviour<NetworkInventory>()
            .TryAddItem(curSession.ItemAuctionInfo.ItemSO.ItemId, curSession.ItemAuctionInfo.Amount);
        //Gửi thông báo kết quả cho toàn bộ
        foreach (var i in curSession.biders)
        {
            string tokenAuctionResult = "";
            if (i.Key == curSession.Winner.Player)
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
            Debug.Log(tokenAuctionResult);
            var data = Encoding.UTF8.GetBytes(tokenAuctionResult);
            Runner.SendReliableDataToPlayer(i.Key, ConstReliableKey.AuctionResult, data);
        }
        RPC_EndSession();
    }

    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if (data.key == ConstReliableKey.AuctionResult)
        {
            string s = Encoding.UTF8.GetString(data.data);
            NotificationManager.Ins.ShowNotification(s);
        }
        if (data.key == ConstReliableKey.Tradings)
        {
            string s = Encoding.UTF8.GetString(data.data);
            TradingResult result = JsonConvert.DeserializeObject<TradingResult>(s);
            if (!result.Success)
            {
                NotificationManager.Ins.ShowNotification(result.Message);
            }
            OnServerRespone_TradingResult?.Invoke(result);
        }
    }

    //RPC.

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndSession()
    {
        AuctionSesstionUI.OnEndSession();
        historySession.Push(curSession);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestTrading(int wood, int stone, int gold, PlayerRef player)
    {
        Trading trading = new Trading
        {
            Player = player,
            wood = wood,
            stone = stone,
            gold = gold,
            DateTime = DateTime.UtcNow
        };

        NetworkPlayer bider = Runner.GetPlayerObject(player).GetBehaviour<NetworkPlayer>();
        if (TradingValid(bider.resourcePlayer, trading))
        {
            var tradingResult = curSession.AddTheTrading(trading);
            if (tradingResult.Success)
            {
                bider.ComsumpResource(new NetworkResourcePlayer
                {
                    Wood = trading.wood,
                    Stone = trading.stone,
                    GoldOre = trading.gold,
                });
            }
            string jsonResult = JsonConvert.SerializeObject(tradingResult);
            Runner.SendReliableDataToPlayer(player, ConstReliableKey.Tradings, Encoding.UTF8.GetBytes(jsonResult));
        }
        else
        {
            string s = $"Yêu cầu thất bại: ".ToColor(Color.orangeRed) + "Tài nguyên không đủ";
            var tradingResult = new TradingResult(false, s);
            string json = JsonConvert.SerializeObject(tradingResult);
            Runner.SendReliableDataToPlayer(player, ConstReliableKey.Tradings, Encoding.UTF8.GetBytes(json));
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_UpdateSession(int itemID, int amount, int startingValue, float seconds)
    {
        if (HasStateAuthority) return;
        ItemAuctionInfo itemAuctionInfo = new ItemAuctionInfo
        {
            ItemSO = itemDatabase.GetItem(itemID),
            Amount = amount,
            StartingValue = startingValue,
        };
        endSessionToken = new CancellationTokenSource();
        curSession = new AuctionSession(this, itemAuctionInfo);
        Debug.Log("RPC_UPdateSessoin Called", this);
        AuctionSesstionUI.UpdateSession(curSession, seconds, endSessionToken.Token);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnAddedTradingEvents(int wood, int stone, int gold, PlayerRef player, byte[] dateTime)
    {

        string s = Encoding.UTF8.GetString(dateTime);
        DateTime time = JsonConvert.DeserializeObject<DateTime>(s);
        Trading infor = new Trading
        {
            wood = wood,
            stone = stone,
            gold = gold,
            Player = player,
            DateTime = time

        };
        AuctionSesstionUI.OnAddedTrading(Runner, infor);
        if (!HasStateAuthority)
            curSession.AddTheTrading(infor);
    }

    public static bool TradingValid(NetworkResourcePlayer resouce, Trading info)
    {
        if (resouce.Wood < info.wood) return false;
        if (resouce.Stone < info.stone) return false;
        if (resouce.GoldOre < info.gold) return false;
        return true;
    }


}

[Serializable]
public class ItemAuctionInfo
{
    public ItemSO ItemSO;
    public int Amount;
    [Tooltip("Giá trị khởi điểm")]
    public int StartingValue;
}

[Serializable]
public struct Trading
{
    //public int PlayerID;
    public PlayerRef Player;
    public int wood;
    public int stone;
    public int gold;
    public DateTime DateTime; //Sever GetSet     
    //public PlayerRef Player
    //{
    //    get => PlayerRef.FromEncoded(PlayerID);
    //    set => PlayerID = value.RawEncoded;
    //}
}


