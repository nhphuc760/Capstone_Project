using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryTradingElement : MonoBehaviour
{
    [SerializeField] Image avatar;
    [SerializeField] Sprite defaultAvt;
    [SerializeField] TextMeshProUGUI namePlayer;
    [SerializeField] TextMeshProUGUI lastTime;
    [SerializeField] TextMeshProUGUI money;    



    public void Clear()
    {
        if(avatar)
            avatar.sprite = defaultAvt;
        if (namePlayer)
            namePlayer.text = "";
        if(lastTime)
        lastTime.text = "";
        if(money)
        money.text = "";
    }

    public void UpdateUI(string namePlayer,int money, string lastTime, Sprite avt = null)
    {
        if(this.namePlayer)
            this.namePlayer.text = namePlayer;
        if (this.money)
            this.money.text = money.ToString();
        if(this.lastTime)
            this.lastTime.text = lastTime;
        if(avt != null)
            avatar.sprite = avt;
    }

    public void UpdateTime(string time)
    {
        if(lastTime)
            lastTime.text = time;
    }


}
