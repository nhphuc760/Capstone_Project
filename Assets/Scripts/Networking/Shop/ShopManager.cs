using System.Linq;
using Fusion;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{

    [SerializeField] Collider bounds;
    [SerializeField] SnowmanNPC npcShop;
    [SerializeField] ItemDatabase shopItemDatabase;


    [OnChangedRender(nameof(OnBannedListChanged))]
    [Networked, Capacity(8)] NetworkArray<PlayerRef> bannedLists => default;
    [OnChangedRender(nameof(OnPlayerInsidedChanged))]
    [Networked, Capacity(8)] NetworkArray<PlayerRef> playerInSide => default;
    public bool IsValid(PlayerRef player) => !bannedLists.Contains(player);



    public override void Spawned()
    {
        Debug.Log(playerInSide.ToString());
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
}
