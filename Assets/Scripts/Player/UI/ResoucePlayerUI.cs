using System;
using TMPro;
using UnityEngine;

public class ResoucePlayerUI : MonoBehaviour
{

    [SerializeField] NetworkPlayer player;
    [SerializeField] TextMeshProUGUI woodAmount;
    [SerializeField] TextMeshProUGUI ironAmount;
    [SerializeField] TextMeshProUGUI copperAmount;
    [SerializeField] TextMeshProUGUI goldAmount;


    void Start()
    {
        player ??= GetComponentInParent<NetworkPlayer>();
        if(player)
        player.OnResouceChange += OnResourceChanged;

    }

    private void OnResourceChanged(NetworkResourcePlayer player)
    {
        woodAmount.text = player.Wood.ToString();
        ironAmount.text = player.IronOre.ToString();
        copperAmount.text = player.CopperOre.ToString();
        goldAmount.text = player.GoldOre.ToString();
    }

    private void OnDestroy()
    {
        if(player != null) 
        player.OnResouceChange -= OnResourceChanged;
    }
}
