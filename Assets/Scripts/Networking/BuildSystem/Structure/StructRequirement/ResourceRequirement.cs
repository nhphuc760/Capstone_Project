using System;
using System.Linq;
using Fusion;
using UnityEngine;

[Serializable]

public class ResourceRequirement : RequirementDefinition
{
    [SerializeField] IconTMP iconTMP;
    public int Wood;
    public int Stone;
    public int Gold;
    public int Electric;
    public override IRequirement CreateRuntime(RequirementServices services, NetworkObject source = default)
    {
        return new ResourceRequirementInstance
        {
            definition = this,
            Player = services.player            
        };
    }



    public override string ToString()
    {
        string s = string.Empty;
        if(iconTMP == null)
        {
            Debug.LogWarning("iconTMP null");
            iconTMP = Resources.LoadAll<IconTMP>("IconTextMeshPro").First();
        }

        s += Wood > 0 ? $"{iconTMP.WoodCode} {Wood}  " : "";
        s += Stone > 0 ? $"{iconTMP.StoneCode} {Stone}  " : "";
        s += Gold > 0 ? $"{iconTMP.GoldCode} {Gold}  " : "";
        s += Electric > 0 ? $"{iconTMP.ElectricCode} {Electric}" : "";
        return s;
    }
    
}

public class ResourceRequirementInstance : IRequirement
{

    public ResourceRequirement definition;
    public NetworkPlayer Player { get; set; }

    public event Action Changed;


    public void Dispose()
    {
        
    }

    public string GetInfor()
    {
        string info = string.Empty;

        info += definition.Wood > 0 ? ($"Wood: " + (definition.Wood.ToString().ToColor(Player.resourcePlayer.Wood > definition.Wood ? Color.green : Color.red)) + "\n") : "";
        info += definition.Stone > 0 ? ($"Stone: " + (definition.Stone.ToString().ToColor(Player.resourcePlayer.Stone > definition.Stone ? Color.green : Color.red)) + "\n") : "";
        info += definition.Gold > 0 ? ($"GoldOre: " + (definition.Gold.ToString().ToColor(Player.resourcePlayer.GoldOre > definition.Gold ? Color.green : Color.red))) : "";
        info += definition.Electric > 0 ? ($"Electric: " + (definition.Electric.ToString().ToColor(Player.resourcePlayer.Electric > definition.Electric ? Color.green : Color.red)) + "\n") : "";
        return info;
    }

    public CheckMetResult Met()
    {
        if (!CompareResource())
        {
            return new CheckMetResult
            {
                Reason = CheckMetReason.NotEnoughResource,
                Message = "Không đủ tài nguyên"
            };
        }
        return default;
    }

    bool CompareResource()
    {
        if (Player.resourcePlayer.Wood < definition.Wood) return false;
        if (Player.resourcePlayer.Stone < definition.Stone) return false;
        if (Player.resourcePlayer.GoldOre < definition.Gold) return false;
        if (Player.resourcePlayer.Electric < definition.Electric) return false;
        return true;
    }
    public void Start()
    {
       
    }

    public void Effect()
    {
        Player.ComsumpResource(new NetworkResourcePlayer
        {
            Wood = definition.Wood,
            Stone = definition.Stone,
            GoldOre = definition.Gold,
            Electric = definition.Electric,
        });
    }
}



