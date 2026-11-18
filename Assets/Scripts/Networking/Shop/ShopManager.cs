using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [SerializeField] Collider bounds;
    [SerializeField] SnowmanNPC npcShop;
    [SerializeField] ItemShopDatabase shopItemDatabase;

    public IconTMP iconTMP;

    [OnChangedRender(nameof(OnBannedListChanged))]
    [Networked, Capacity(8)] NetworkArray<PlayerRef> bannedLists => default;
    [OnChangedRender(nameof(OnPlayerInsidedChanged))]
    [Networked, Capacity(8)] NetworkArray<PlayerRef> playerInSide => default;
    public bool IsValid(PlayerRef player) => !bannedLists.Contains(player);
    EventBinding<ReliableReceiveData> onBuyToken;

    public List<PlayerRef> BannedLists => bannedLists.ToList();

    public override void Spawned()
    {
        Debug.Log(playerInSide.ToString());
        onBuyToken = new EventBinding<ReliableReceiveData>(OnReliableReceiveData);
        EventBus<ReliableReceiveData>.Register(onBuyToken);
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        EventBus<ReliableReceiveData>.Deregister(onBuyToken);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;
        Debug.Log("Collider with " + other.transform.root.name);
        if (other.transform.root.TryGetComponent<NetworkPlayer>(out var player))
        {
            if (playerInSide.Contains(player.Object.InputAuthority)) return;
            for(int i = 0; i < 8; i++)
            {
                if (playerInSide[i] == null || playerInSide[i] == PlayerRef.None) 
                { 
                    playerInSide.Set(i, player.Object.InputAuthority);
                    player.Health.OnTakeDamage += TriggerDamage;    
                    break;
                }
            }         
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if(!HasStateAuthority) return;
        if(other.transform.root.TryGetComponent<NetworkPlayer>(out var player))
        {
            for (int i = 7; i >= 0; i--)
            {
                if (playerInSide[i] == player.Object.InputAuthority)
                {
                    playerInSide.Set(i, PlayerRef.None);
                    player.Health.OnTakeDamage -= TriggerDamage;
                    break;
                }
            }           
        }
    }

    void OnBannedListChanged()
    {
        if (bannedLists.Contains(Runner.LocalPlayer))
        {
            npcShop.ShowUI(false);
        }
    }

    void OnPlayerInsidedChanged()
    {
        Debug.Log(playerInSide.ToString());
    }

    void TriggerDamage(NetworkObject attacker)
    {
        for (int i = 0; i < 8; i++)
        {
            if (bannedLists[i] == null || bannedLists[i] == PlayerRef.None)
            {
                bannedLists.Set(i, attacker.InputAuthority);
                break;
            }
        }
    }   

    void OnDayStart()
    {
        bannedLists.Clear();
        AddPlayerInsided();
    }

    
    void OnNightStart()
    {
        RemovePlayerStillInside();
        playerInSide.Clear();
        //Add item trading
    }



    void AddPlayerInsided()
    {
        int i = 0;
        var colliders = Physics.OverlapBox(bounds.bounds.center, bounds.bounds.extents, Quaternion.identity);
        for (int j = 0; j < colliders.Length; j ++)
        {
            if (colliders[i].transform.root.TryGetComponent<NetworkPlayer>(out var player))
            {
                player.Health.OnTakeDamage += TriggerDamage;
                playerInSide.Set(i, player.Object.InputAuthority);
                i++;
            }
        }       
    }

    void RemovePlayerStillInside()
    {
        foreach (var i in playerInSide)
        {
            if (i == null || i == PlayerRef.None) continue;
            Runner.GetPlayerObject(i).GetBehaviour<NetworkPlayer>().Health.OnTakeDamage -= TriggerDamage;
        }
    }   
    

    public void BuyItem(ItemShop itemShop)
    {
        int indexOfShop = shopItemDatabase.itemShops.IndexOf(itemShop);
        RPC_RequestBuyItem(indexOfShop, Runner.LocalPlayer);
    }

    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if(data.key == ConstReliableKey.BuyResult)
        {
            string s = Encoding.UTF8.GetString(data.data);
            Debug.Log("Debug Result: " + s);
            var message = s.Split(',');
            if(message.Length > 1)
            {
                int flags = 0;
                if (message[1] == "!")
                    flags = 2;
                else
                if (message[1] == "v")
                    flags = 1;
                NotificationManager.Ins.ShowNotification(message[0], flags);
            }
            else
            {
                NotificationManager.Ins.ShowNotification(message[0]);
            }
        }
    }

    

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestBuyItem(int index, PlayerRef player)
    {
        if (!IsValid(player))
        {
            string s = "Bạn đang bị cấm mua bán";
            Runner.SendReliableDataToPlayer(player, ConstReliableKey.BuyResult, Encoding.UTF8.GetBytes(s));
            return;
        }

        if(index >= shopItemDatabase.itemShops.Count)
        {
            //false
            string s = "Lỗi bất đồng bộ dữ liệu";
            Runner.SendReliableDataToPlayer(player, ConstReliableKey.BuyResult, Encoding.UTF8.GetBytes(s));
            return;
        }
        ItemShop itemShop = shopItemDatabase.itemShops[index];
        if(itemShop != null)
        {
            //Check resource
            var playerObj = Runner.GetPlayerObject(player).GetBehaviour<NetworkPlayer>();
            string s = "";
            if (playerObj.resourcePlayer >= itemShop.price)
            {
                playerObj.ComsumpResource(itemShop.price);
                playerObj.GetBehaviour<NetworkInventory>().TryAddItem(itemShop.itemSO.ItemId, 1);
                s = "Mua thành công. ".ToColor(Color.green) + $"Bạn nhận được {itemShop.itemSO.ItemName},v";
            }
            else
            {
                s = "Không đủ tài nguyên,!";
            }
                Runner.SendReliableDataToPlayer(player, ConstReliableKey.BuyResult, Encoding.UTF8.GetBytes(s));
        }
    }


}
