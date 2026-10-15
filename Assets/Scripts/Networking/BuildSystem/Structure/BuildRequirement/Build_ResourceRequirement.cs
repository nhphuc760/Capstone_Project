using System;
using Fusion;
[Serializable]
public class Build_ResourceRequirement : EntityValueBuildRequirement
{    


    public override BuildRequirementResult CheckRequirement(NetworkRunner runner, PlayerRef player)
    {
        if (runner.TryGetPlayerObject(player, out NetworkObject playerObj))
        {
            var resourcePlayer = playerObj.GetBehaviour<NetworkPlayer>().resourcePlayer;
            if (CompareResource(resourcePlayer))
            {
                return new BuildRequirementResult { Reason = BuildFailReason.None, Message = "" };
            }
            else
            {
                return new BuildRequirementResult { Reason = BuildFailReason.NotEnoughResource, Message = "Không đủ tài nguyên" };
            }
        }
        else
        {
            return new BuildRequirementResult
            {
                Reason = BuildFailReason.InvalidExecute,
                Message = "Lỗi thực thi"
            };
        }

    }

    public override string GetInfo(PlayerRef player)
    {
        throw new System.NotImplementedException();
    }

    public override void OnBuildExecute(NetworkRunner runner, PlayerRef playerRef)
    {
        NetworkPlayer networkPlayer = runner.GetPlayerObject(playerRef).GetBehaviour<NetworkPlayer>();
        int Wood = 0;
        int IronOre = 0;
        int CopperOre = 0;
        int GoldOre = 0;
        foreach (var i in values)
        {

            switch (i.Type)
            {
                case EntityValueType.Wood:
                    Wood = i.Value; break;
                case EntityValueType.IronOre:
                    IronOre = i.Value; break;
                case EntityValueType.CopperOre:
                    CopperOre = i.Value; break;
                case EntityValueType.Gold:
                    GoldOre = i.Value; break;
                default: break;
            }
        }
        networkPlayer.ComsumpResource(new NetworkResourcePlayer 
        {
            Wood = Wood,
            IronOre = IronOre,
            CopperOre = CopperOre,
            GoldOre = GoldOre,
        });

    }

    bool CompareResource(NetworkResourcePlayer resourcePlayer)
    {
        int Wood = 0;
        int IronOre = 0;
        int CopperOre = 0;
        int GoldOre = 0;      
        foreach (var i in values)
        {
            
            switch (i.Type) 
            {
                case EntityValueType.Wood:
                    Wood = i.Value; break;  
                case EntityValueType.IronOre:
                    IronOre = i.Value; break;
                case EntityValueType.CopperOre:
                    CopperOre = i.Value; break;
                case EntityValueType.Gold:
                    GoldOre = i.Value; break;
                default: break;                
            }

        }
        if (resourcePlayer.Wood < Wood) return false;
        if (resourcePlayer.IronOre < IronOre) return false;
        if (resourcePlayer.CopperOre < CopperOre) return false;
        if (resourcePlayer.GoldOre < GoldOre) return false;
        return true;
    }



}
