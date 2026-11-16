using System;
using Fusion;
using UnityEngine;

public class SnowmanNPC : MonoBehaviour
{
    [SerializeField] ShopManager Shop;
    [SerializeField] GameObject ShopUI;
    [SerializeField] NetworkObject networkObj;   
    NetworkPlayer player;
    //quick test

    private void Start()
    {
      
    }

    void MakesureHasPlayer()
    {
        player = player != null ? player : networkObj.Runner.GetPlayerObject(networkObj.Runner.LocalPlayer).GetBehaviour<NetworkPlayer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            MakesureHasPlayer();
            if (player.Object.HasInputAuthority)
            {
               
            }
        }

    }



    


    private void OnDestroy()
    {
        
    }  

    public void ShowUI(bool value)
    {
        ShopUI?.SetActive(value);
    }

}
