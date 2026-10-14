using System;
using Fusion;
[Serializable]
public class Build_ResourceRequirement : BuildRequirement
{

    public int Wood;
    public int IronOre;
    public int CopperOre;
    public int GoldOre;

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


    bool CompareResource(NetworkResourcePlayer resourcePlayer)
    {
        if (resourcePlayer.Wood < Wood) return false;
        if (resourcePlayer.IronOre < IronOre) return false;
        if (resourcePlayer.CopperOre < CopperOre) return false;
        if (resourcePlayer.GoldOre < GoldOre) return false;
        return true;
    }



}
