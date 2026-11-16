using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuctionHistorySessionUI : MonoBehaviour
{
    [SerializeField] AuctionManager manager;
    [SerializeField] Transform containerSession;
    [SerializeField] GameObject GeneralContent;

    [Header("Items")]
    [SerializeField] Image ItemIcon;
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI description;
    [SerializeField] TextMeshProUGUI startingValue;

    [Header("Winners")]

    [SerializeField] Image Avatar;
    [SerializeField] Sprite defaultAvt;
    [SerializeField] TextMeshProUGUI NameWinner;
    [SerializeField] TextMeshProUGUI totalMoney;

    [Header("HistoryList")]
    [SerializeField] Transform historyTradingContainer;
    [SerializeField] GameObject sessionNone;


    HistorySessionElement[] historySessionElements;
    HistoryTradingElement[] historyTradingElements;
      
    private void Awake()
    {
        var listTradingElement = new List<HistoryTradingElement>();
        for (int i = 0; i < historyTradingContainer.childCount; i++)
        {
            Transform child = historyTradingContainer.GetChild(i);
            listTradingElement.Add(child.GetComponent<HistoryTradingElement>());
            child.gameObject.SetActive(false);
        }
        historyTradingElements = listTradingElement.ToArray();

        var listSessionHistory = new List<HistorySessionElement>();
        for (int i = 0; i < containerSession.childCount; i++)
        {
            Transform child = containerSession.GetChild(i);
            HistorySessionElement historySessionElement = child.GetComponent<HistorySessionElement>();
            listSessionHistory.Add(historySessionElement);
            child.gameObject.SetActive(false);
        }
        historySessionElements = listSessionHistory.ToArray();

    }


    private void OnEnable()
    {
        ClearHistorySession();       
        sessionNone.SetActive(manager.HistorySession.Count <= 0);
        foreach (var i in manager.HistorySession)
        {
            var sessionElement = GetInActiveSessionElement();
            if(sessionElement != null)
            {
                sessionElement.SetSession(i, this, manager);
                sessionElement.gameObject.SetActive(true);
            }
        }
    }





    public void OnPointerClickElement(AuctionSession session)
    {
        GeneralContent.SetActive(true);
        UpdateUI(session);
    }

    void UpdateUI(AuctionSession session)
    {
        UpdateHistoryTradingList(session);
        ItemIcon.sprite = session.ItemAuctionInfo.ItemSO.Icon;
        ItemName.text = session.ItemAuctionInfo.ItemSO.ItemName;
        description.text = session.ItemAuctionInfo.ItemSO.Description;
        startingValue.text = session.ItemAuctionInfo.StartingValue.ToString();

        Debug.Log( "Client" + session.Winner.Player, this);
        string nameWinner = manager.Runner.GetPlayerObject(session.Winner.Player).GetBehaviour<NetworkPlayer>().Name.Value;
        NameWinner.text = string.IsNullOrEmpty(nameWinner) ? session.Winner.Player.ToString() : nameWinner;
        totalMoney.text = manager.ConvertToMoney(session.Winner.wood, session.Winner.stone, session.Winner.gold).ToString();
    }


    void ClearHistoryTrading()
    {
        for (int i = historyTradingContainer.childCount - 1; i >= 0; i--)
        {
            var child = historyTradingContainer.GetChild(i);
            child.GetComponent<HistoryTradingElement>().Clear();
            child.gameObject.SetActive(false);
        }
    }

    void ClearHistorySession()
    {
        for (int i = containerSession.childCount - 1; i >= 0; i--)
        {
            var child = containerSession.GetChild(i);
            child.gameObject.SetActive(false);
        }
    }

    void UpdateHistoryTradingList(AuctionSession session)
    {
        Debug.Log("Update History trading list");
        ClearHistoryTrading();
        var tradingslist = session.HistoryTradings;

        for (int i = 0; i < tradingslist.Count; i++)
        {
            var elementUI = GetInActiveTradingElement();
            if(elementUI != null)
            {
                elementUI.gameObject.SetActive(true);
                Trading tradingInfo = tradingslist[i];
                NetworkPlayer player = manager.Runner.GetPlayerObject(tradingInfo.Player).GetBehaviour<NetworkPlayer>();
                var timeSpan = DateTime.UtcNow - tradingInfo.DateTime;
                string lastTime = timeSpan.Minutes > 0 ? $"{timeSpan.Minutes} phút trước" : $"{timeSpan.Seconds} giây trước";
                elementUI.UpdateUI(string.IsNullOrEmpty(player.Name.Value) ? tradingInfo.Player.ToString() : player.Name.Value, 
                    manager.ConvertToMoney(tradingInfo.wood, tradingInfo.stone, tradingInfo.gold), 
                    lastTime);
            }
        }

    }

    
    HistoryTradingElement GetInActiveTradingElement()
    {      

        foreach (var i in historyTradingElements)
        {
            if (!i.gameObject.activeSelf) return i;
        }

        return historyTradingElements.Last();
    }

    HistorySessionElement GetInActiveSessionElement()
    {

        foreach (var i in historySessionElements)
        {
            if (!i.gameObject.activeSelf) return i;
        }

        return historySessionElements.Last();
    }

}
