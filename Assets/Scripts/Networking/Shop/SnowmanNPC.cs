using Fusion;
using UnityEngine;

public class SnowmanNPC : MonoBehaviour
{
    public static SnowmanNPC instance;
    [SerializeField] ShopManager Shop;
    [SerializeField] GameObject ShopUI;
    [SerializeField] NetworkObject networkObj;   
    [SerializeField] NetworkQuestSys QuestSystem;
    NetworkPlayer player;
    //quick test

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //NetworkQuestSys QuestSystem = player.GetComponent<NetworkQuestSys>();
    }

    void MakesureHasPlayer()
    {
        player = player != null ? player : networkObj.Runner.GetPlayerObject(networkObj.Runner.LocalPlayer).GetBehaviour<NetworkPlayer>();
    }

    public void Interact()
    {
        MakesureHasPlayer();

        QuestSystem.RequestAcceptFixedQuest();
    }
    

    private void OnDestroy()
    {
        
    }  

    public void ShowUI(bool value)
    {
        ShopUI?.SetActive(value);
    }

    public void ToggleShopUI()
    {       
        ShopUI?.SetActive(!ShopUI.activeSelf);
    }

}
