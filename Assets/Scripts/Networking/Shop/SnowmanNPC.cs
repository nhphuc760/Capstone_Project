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

    void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            Interact();
        }
    }

    void MakesureHasPlayer()
    {
        player = player != null ? player : networkObj.Runner.GetPlayerObject(networkObj.Runner.LocalPlayer).GetBehaviour<NetworkPlayer>();

        Debug.Log("[Test]Player: " + player);

        QuestSystem = player.GetComponent<NetworkQuestSys>();
    }

    void PlayerDistanceCheck()
    {
        float distance = Vector3.Distance(player.transform.position, transform.position);

        if (distance <= 5f)
        {
            QuestSystem.RequestAcceptFixedQuest();
        }
        else if (distance > 5f)
        {
            Debug.Log("[Test]Player cách snowman: " + distance);
            return;
        }

    }

    public void Interact()
    {
        MakesureHasPlayer();
        PlayerDistanceCheck();
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
        if(ShopUI.activeSelf)
        {
            if(BackgroundMusicManager.Instance != null)
            {
                BackgroundMusicManager.Instance.OnShopOpen();
            }
        }
        else
        {
            if (BackgroundMusicManager.Instance != null)
            {
                BackgroundMusicManager.Instance.OnShopClose();
            }

        }


    }

}
