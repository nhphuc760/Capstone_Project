using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
public class AuctionSession
{
    public readonly ItemAuctionInfo ItemAuctionInfo;
    readonly AuctionManager manager;
    readonly HistoryStore<Trading> historyTradings = new HistoryStore<Trading>(10);

    Dictionary<PlayerRef, Trading> totalMoneyBiders = new Dictionary<PlayerRef, Trading>();




    public event Action<Trading> OnHistoryTradingAdded;

    public IReadOnlyList<Trading> HistoryTradings => historyTradings.List;
    public Dictionary<PlayerRef, Trading> biders => totalMoneyBiders;
    int onlySet = 0;
    public Trading Winner { get; private set; }
    Trading HighestBider;



    public AuctionSession(AuctionManager manager, ItemAuctionInfo itemAuction)
    {
        this.manager = manager;
        this.ItemAuctionInfo = itemAuction;
    }


    public TradingResult AddTheTrading(Trading tradingInfo)
    {

        //Kiểm tra và lấy money sẵn có + với trading và so sánh với highest.
        int money = manager.ConvertToMoney(tradingInfo.wood, tradingInfo.stone, tradingInfo.gold);
        if (totalMoneyBiders.TryGetValue(tradingInfo.Player, out Trading value))
        {
            var totalMoney = manager.ConvertToMoney(tradingInfo.wood + value.wood, tradingInfo.stone + value.stone, tradingInfo.gold + value.gold);
            money += totalMoney;
        }
        //Nếu nhỏ hơn giá khởi điểm thì false
        if (money <= ItemAuctionInfo.StartingValue)
        {
            return new TradingResult
            {
                Success = false,
                Message = "Yêu cầu tiền cược lớn hơn giá khởi điểm"
            };
        }
        //So sánh với highest

        if (HighestBider.Player == tradingInfo.Player)
        {
            return new TradingResult
            {
                Success = false,
                Message = "Bạn đang đứng đầu mức cược."
            };
        }


        if (HighestBider.Player != PlayerRef.None && money <= manager.ConvertToMoney(HighestBider.wood, HighestBider.stone, HighestBider.gold))
        {
            return new TradingResult
            {
                Success = false,
                Message = "Yêu cầu thất bại. ".ToColor(Color.orangeRed) + "Cần nhiều tiền hơn!"
            };
        }
        //Tính tổng và tạo một trading mới
        var totalTrading = new Trading
        {
            Player = tradingInfo.Player,
            wood = tradingInfo.wood + value.wood,
            stone = tradingInfo.stone + value.stone,
            gold = tradingInfo.gold + value.gold,
            DateTime = tradingInfo.DateTime,
        };

        totalMoneyBiders[tradingInfo.Player] = totalTrading;
        historyTradings.Add(totalTrading);
        OnHistoryTradingAdded?.Invoke(totalTrading);
        HighestBider = totalTrading;

        return new TradingResult(true);
    }

    public void EndSession()
    {
        if (onlySet != 0) return;
        Winner = HighestBider;
        onlySet = 1;
    }





    //Client only
    /// <summary>
    /// Client only
    /// </summary>
    /// <param name="overrideData"></param>
    public void ApplyOverrideHistoryData(NetworkRunner runner, IReadOnlyList<Trading> overrideData)
    {
        if (runner.IsServer) return;
        for (int i = 0; i < 10; i++)
        {
            historyTradings.Set(i, overrideData[i]);
        }
    }



}

[Serializable]
public struct TradingResult
{
    public bool Success;
    public string Message;
    public TradingResult(bool success, string message = null)
    {
        this.Success = success;
        this.Message = message;
    }
}


public class HistoryStore<T>
{
    private readonly int _maxSize;
    private readonly List<T> _items = new();
    public int Count => _items.Count;
    public HistoryStore(int maxSize)
    {
        _maxSize = maxSize;
    }


    public void Set(int index, T value)
    {
        _items[index] = value;
    }
    public void Add(T item)
    {
        // Xóa phần tử cuối nếu vượt giới hạn
        if (_items.Count >= _maxSize)
        {
            _items.RemoveAt(_items.Count - 1);
        }

        // Thêm vào đầu danh sách
        _items.Insert(0, item);
    }


    public void Clear() => _items.Clear();

    public T this[int index]
    {
        get => _items[index];
        private set => _items[index] = value;
    }

    public IReadOnlyList<T> List => _items;
}




