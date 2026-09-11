using System;
using Fusion;
using UnityEngine;

[Serializable]
public class ResourceRequirement : StructRequirement
{
    public int Wood;
    public int IronOre;
    public int CopperOre;
    public int GoldOre;



    public override UpgradeResult CheckRequirement()
    {
        PlayerRef player = structAuthority.Object.InputAuthority;
        if (structAuthority.Object.Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
        {
            NetworkResourcePlayer resourcePlayer = playerObj.GetBehaviour<NetworkPlayer>().resourcePlayer;
            if (CompareResource(resourcePlayer))
            {
                return new UpgradeResult { Reason = UpgradeFailReason.None, Message = "" };
            }
            return new UpgradeResult { Reason = UpgradeFailReason.NotEnoughResource, Message = "Không đủ tài nguyên" };
        }
        else
        {
            Debug.LogWarning("Lỗi không tìm thấy PlayerObject");
            return default;
        }
    }

    public override StructRequirement CreateInstance(StructureBase structBase)
    {
        return new ResourceRequirement 
        { 
            structAuthority = structBase,
            Wood = this.Wood,
            IronOre = this.IronOre,
            CopperOre = this.CopperOre,
            GoldOre = this.GoldOre,
        };
    }

    public override void Destroy()
    {
      
    }

    public override string Information()
    {
        string info = string.Empty;
        if (structAuthority.Object.Runner.TryGetPlayerObject(structAuthority.Object.InputAuthority, out NetworkObject playerObj))
        {
            NetworkResourcePlayer resourcePlayer = playerObj.GetBehaviour<NetworkPlayer>().resourcePlayer;
            info += Wood > 0 ? ($"Wood: " + (Wood.ToString().ToColor(resourcePlayer.Wood > Wood ? Color.green : Color.red)) + "\n") : "";
            info += IronOre > 0 ? ($"IronOre: " + (IronOre.ToString().ToColor(resourcePlayer.IronOre > IronOre ? Color.green : Color.red)) + "\n") : "";
            info += CopperOre > 0 ? ($"CopperOre: " + (CopperOre.ToString().ToColor(resourcePlayer.CopperOre > CopperOre ? Color.green : Color.red)) + "\n") : "";
            info += GoldOre > 0 ? ($"GoldOre: " + (GoldOre.ToString().ToColor(resourcePlayer.GoldOre > GoldOre ? Color.green : Color.red))) : "";
        }
        return info;        
    }

    bool CompareResource(NetworkResourcePlayer resourcePlayer)
    {
        if(resourcePlayer.Wood < Wood) return false;
        if(resourcePlayer.IronOre < IronOre) return false;
        if(resourcePlayer.CopperOre < CopperOre) return false;
        if(resourcePlayer.GoldOre < GoldOre) return false;
        return true;
    }

}
