using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using UnityEngine;

[Serializable]

public class ResourceRequirement : RequirementDefinition
{
    public IconTMP iconTMP;
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
        if(iconTMP == null)
        {
            Debug.LogWarning("iconTMP null");
            iconTMP = Resources.LoadAll<IconTMP>("IconTextMeshPro").First();
        }

        List<string> infos = new List<string>();

        if (Wood > 0) infos.Add($"{iconTMP.WoodCode} {Wood}");
        if (Stone > 0) infos.Add($"{iconTMP.StoneCode} {Stone}");
        if (Gold > 0) infos.Add($"{iconTMP.GoldCode} {Gold}");
        if (Electric > 0) infos.Add($"{iconTMP.ElectricCode} {Electric}");

        List<string> lines = new List<string>();

        for (int i = 0; i < infos.Count; i += 2)
        {
            lines.Add(string.Join("  ", infos.Skip(i).Take(2)));
        }

        return string.Join("\n", lines);
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
        List<string> infos = new List<string>();

        if (definition.Wood > 0)
            infos.Add($"{definition.iconTMP.GetResourceCode(ResourceType.Wood)}: " +
                definition.Wood.ToString().ToColor(Player.resourcePlayer.Wood >= definition.Wood ? Color.green : Color.red));

        if (definition.Stone > 0)
            infos.Add($"{definition.iconTMP.GetResourceCode(ResourceType.Stone)}: " +
                definition.Stone.ToString().ToColor(Player.resourcePlayer.Stone >= definition.Stone ? Color.green : Color.red));

        if (definition.Gold > 0)
            infos.Add($"{definition.iconTMP.GetResourceCode(ResourceType.Gold)}: " +
                definition.Gold.ToString().ToColor(Player.resourcePlayer.GoldOre >= definition.Gold ? Color.green : Color.red));

        if (definition.Electric > 0)
            infos.Add($"{definition.iconTMP.GetResourceCode(ResourceType.Electric)}: " +
                definition.Electric.ToString().ToColor(Player.resourcePlayer.Electric >= definition.Electric ? Color.green : Color.red));

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < infos.Count; i++)
        {
            result.Append(infos[i]);

            if (i % 2 == 0 && i + 1 < infos.Count)
                result.Append("  ");

            else if (i % 2 == 1 && i + 1 < infos.Count)
                result.AppendLine();
        }

        return result.ToString();
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



