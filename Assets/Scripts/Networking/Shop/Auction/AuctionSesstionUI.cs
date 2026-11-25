using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuctionSesstionUI : MonoBehaviour
{
    [SerializeField] Transform containerHistoryTrading;
    [SerializeField] float updateTimeTradingInterval = 10f;
    [SerializeField] AuctionManager manager;
    [SerializeField] Image itemIcon;
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] TextMeshProUGUI description;
    [SerializeField] TextMeshProUGUI startingValue;
    [SerializeField] TextMeshProUGUI timer;

    [Header("Inputs Resource")]
    [SerializeField] IntValueInput woodInput;
    [SerializeField] IntValueInput stoneInput;
    [SerializeField] IntValueInput goldInput;
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] Button allIn;
    [SerializeField] Button trade;
    [SerializeField] Button clear;
    AuctionSession curSession;
    CancellationTokenSource updateTimeHistoryToken;
    HistoryTradingElement[] historyTradingElements;

    HistoryStore<Trading> historyTradingCache = new HistoryStore<Trading>(10);
    Dictionary<PlayerRef, Trading> totalTradingCache = new Dictionary<PlayerRef, Trading>();

    bool IsTrading;
    private void Awake()
    {
        var listChilds = new List<HistoryTradingElement>();
        for (int i = 0; i < containerHistoryTrading.childCount; i++)
        {
            Transform child = containerHistoryTrading.GetChild(i);  
            listChilds.Add(child.GetComponent<HistoryTradingElement>());            
            child.gameObject.SetActive(false);
        }
        historyTradingElements = listChilds.ToArray();
    }

    private void Start()
    {
        SubcribeInputField();
        SubcribeButton();
        SubcribeManagerEvent();
    }

    private void OnEnable()
    {
        updateTimeHistoryToken = new CancellationTokenSource();
        UpdateTimeHistoryTrading();
        ClearInput();
        SetMinMaxInput();
        Util.Core.Utils.IntervalLoop(UpdateTimeHistoryTrading, (int)(updateTimeTradingInterval * 1000), token: updateTimeHistoryToken.Token).Forget();
        if(BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.OnTradingOpen();
        }

    }

    private void OnDisable()
    {
        if (updateTimeHistoryToken != null)
        {
            updateTimeHistoryToken.Cancel();
            updateTimeHistoryToken = null;
        }
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.OnTradingClose();
        }
    }

    private void OnDestroy()
    {
        DesubcribeInputField();
        DesubcribeButton();
        DesubcribeManagerEvent();
    }


    #region DISPLAY

    public void UpdateSession(AuctionSession session, float seconds, CancellationToken endSessionToken)
    {
        curSession = session;
        ItemSO itemSO = session.ItemAuctionInfo.ItemSO;
        if (itemIcon)
            itemIcon.sprite = itemSO.Icon;
        if (itemName)
            itemName.text = itemSO.ItemName;
        if (description)
            description.text = itemSO.Description;
        if (startingValue)
            startingValue.text = curSession.ItemAuctionInfo.StartingValue.ToString();

        var timeSpan = TimeSpan.FromSeconds(seconds);

        if(timer)
            timer.text = timeSpan.ToString(@"mm\:ss");
        //Start Interval
        Util.Core.Utils.IntervalLoop(UpdateTimeSession, 1000, token: endSessionToken).Forget();

        foreach (var i in historyTradingElements)
        {
            i.Clear();
            i.gameObject.SetActive(false);
        }
        ClearInput();
        ResetButton();
        historyTradingCache.Clear();
    }


    void UpdateTimeSession()
    {
        if (timer)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(manager.RemainingTimeSession);
            timer.text = timeSpan.ToString(@"mm\:ss");
        }
    }



    void UpdateTimeHistoryTrading()
    {
        for (int i = 0; i < historyTradingCache.Count; i++)
        {
            var timeSpan = (DateTime.UtcNow - historyTradingCache[i].DateTime);
            string lastTime = "";
            if(timeSpan.Minutes > 0)
            {
                lastTime = $"{timeSpan.Minutes} phút trước";
            }
            else
            {
                lastTime = $"{timeSpan.Seconds} giây trước";
            }
            historyTradingElements[i].UpdateTime(lastTime);
        }
    }

   
    //Events callback

    public void OnAddedTrading(NetworkRunner runner, Trading trading)
    {
        var element = GetIndexInactiveElement();
        if(element != null)
        {
            element.gameObject.SetActive(true);
            element.Clear();
            string name = runner.GetPlayerObject(trading.Player).GetBehaviour<NetworkPlayer>().Name.Value;
            int money = manager.ConvertToMoney(trading.wood, trading.stone, trading.gold);
            var timeSpan = (trading.DateTime - DateTime.UtcNow);
            string lastTime = "";
            if(timeSpan.Minutes > 0)
            {
                lastTime = $"{timeSpan.Minutes} phút trước";
            }
            else
            {
                lastTime = $"{timeSpan.Seconds} giây trước";
            }           
            element.UpdateUI(string.IsNullOrEmpty(name) ? trading.Player.ToString() : name, money, lastTime);
            //Add data
            historyTradingCache.Add(trading);
            totalTradingCache[trading.Player] = trading;
            element.transform.SetAsFirstSibling();
        }
    }

    #endregion

    #region EXTENSION METHOD
    HistoryTradingElement GetIndexInactiveElement()
    {
        foreach (var i in historyTradingElements)
        {
            if (!i.gameObject.activeSelf) return i;
        }

        return historyTradingElements.Last();
    }

    void SetMinMaxInput()
    {
        NetworkPlayer player = manager.Runner.GetPlayerObject(manager.Runner.LocalPlayer).GetBehaviour<NetworkPlayer>();
        woodInput.SetMinMaxValue(0, player.resourcePlayer.Wood);
        stoneInput.SetMinMaxValue(0, player.resourcePlayer.Stone);
        goldInput.SetMinMaxValue(0, player.resourcePlayer.GoldOre);
    }

    void ResetButton()
    {
        trade.interactable = true;
        allIn.interactable = true;
    }
    #endregion

    #region CONVERT RESOURCE


    void SubcribeInputField()
    {
        if(woodInput)
        woodInput.OnValueChanged += OnInputChange;
        if(stoneInput)
        stoneInput.OnValueChanged += OnInputChange;
        if(goldInput) 
        goldInput.OnValueChanged += OnInputChange;
    }

    void DesubcribeInputField()
    {
        if (woodInput)
            woodInput.OnValueChanged -= OnInputChange;
        if (stoneInput)
            stoneInput.OnValueChanged -= OnInputChange;
        if (goldInput)
            goldInput.OnValueChanged -= OnInputChange;
    }   


    void OnInputChange(int value)
    {
        int money = manager.ConvertToMoney(woodInput.Value, stoneInput.Value, goldInput.Value);
        string s = "";
        if (totalTradingCache.TryGetValue(manager.Runner.LocalPlayer, out Trading previousTrading))
        {
            int preMoney = manager.ConvertToMoney(previousTrading.wood, previousTrading.stone, previousTrading.gold);          
            s = preMoney.ToString() + $"(+ {money})";            
        }
        else
        {
            s = money.ToString();
        }

        if (moneyText)
        {
            moneyText.text = s;
        }


    }  

    void ClearInput()
    {
        if (woodInput) woodInput.SetValue(0, true);
        if(stoneInput) stoneInput.SetValue(0, true);
        if (goldInput) goldInput.SetValue(0, true);
    }

    #endregion

    #region BUTTON LOGIC

    void SubcribeButton()
    {
        allIn.onClick.AddListener(AllIn);
        trade.onClick.AddListener(Trade);
        clear.onClick.AddListener(ClearInput);
    }

    void DesubcribeButton()
    {
        allIn.onClick.RemoveListener(AllIn);
        trade.onClick.RemoveListener(Trade);
        clear.onClick.RemoveListener(ClearInput);
    }

    void AllIn()
    {
        NetworkPlayer player = manager.Runner.GetPlayerObject(manager.Runner.LocalPlayer).GetBehaviour<NetworkPlayer>();
        woodInput.SetValue(player.resourcePlayer.Wood, true);
        stoneInput.SetValue(player.resourcePlayer.Stone, true);
        goldInput.SetValue(player.resourcePlayer.GoldOre, true);
    }

    void Trade()
    {
        if (curSession == null || IsTrading) return;
        IsTrading = true;
        trade.interactable = false;
        if (manager.ConvertToMoney(woodInput.Value, stoneInput.Value, goldInput.Value) <= 0) return;
        manager.RPC_RequestTrading(woodInput.Value, stoneInput.Value, goldInput.Value, manager.Runner.LocalPlayer);
    }

    #endregion

    #region Networking

    void SubcribeManagerEvent()
    {
        manager.OnServerRespone_TradingResult += OnServerRespone_TradingResult;
    }

    void DesubcribeManagerEvent()
    {
        manager.OnServerRespone_TradingResult -= OnServerRespone_TradingResult;
    }

    public void OnServerRespone_TradingResult(TradingResult result)
    {
        IsTrading = false;
        trade.interactable = true;
        if (result.Success)
        {
            SetMinMaxInput();
            woodInput.SetValue(woodInput.Value, true); // hàm ép ui cập nhật 
        }
    }


    public void OnEndSession()
    {
        curSession.ApplyOverrideHistoryData(manager.Runner, historyTradingCache.List);
        curSession.EndSession();
    }

    #endregion

}
