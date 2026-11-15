using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HistorySessionElement : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image Icon;
    [SerializeField] TextMeshProUGUI NameItem;
    [SerializeField] TextMeshProUGUI TotalMoney;
    AuctionHistorySessionUI managerUI;
    AuctionSession session;

    public void OnPointerClick(PointerEventData eventData)
    {
        managerUI.OnPointerClickElement(session);
    }    

    public void SetSession(AuctionSession session, AuctionHistorySessionUI managerUI, AuctionManager manager)
    {
        this.session = session;
        this.managerUI = managerUI;
        Icon.sprite = session.ItemAuctionInfo.ItemSO.Icon;
        NameItem.text = session.ItemAuctionInfo.ItemSO.ItemName;
        int moneyWinner = manager.ConvertToMoney(session.Winner.wood, session.Winner.stone, session.Winner.gold);
        TotalMoney.text = moneyWinner.ToString();
    }


    


}
