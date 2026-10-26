using System;
using TMPro;
using UnityEngine;

public class ResoucePlayerUI : MonoBehaviour
{

    [SerializeField] NetworkPlayer player;
    [SerializeField] TextMeshProUGUI woodAmount;
    [SerializeField] TextMeshProUGUI stoneAmount;
    [SerializeField] TextMeshProUGUI electricAmount;
    [SerializeField] TextMeshProUGUI goldAmount;


    void Start()
    {
        player ??= GetComponentInParent<NetworkPlayer>();
        if(player)
        player.OnResouceChange += OnResourceChanged;

    }

    private void OnResourceChanged(NetworkResourcePlayer player)
    {
        if(player.Wood > 0)
            woodAmount.text = player.Wood.ToString();
        if(player.Stone > 0)
            stoneAmount.text = player.Stone.ToString();
        if(player.GoldOre > 0)
            goldAmount.text = player.GoldOre.ToString();
        if(player.Electric > 0)
            electricAmount.text = player.Electric.ToString();
    }

    private void OnDestroy()
    {
        if(player != null) 
            player.OnResouceChange -= OnResourceChanged;
    }
}
