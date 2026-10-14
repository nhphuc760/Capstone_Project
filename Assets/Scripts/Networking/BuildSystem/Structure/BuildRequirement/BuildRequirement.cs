using System;
using Fusion;

[Serializable]
public abstract class BuildRequirement
{

    public abstract BuildRequirementResult CheckRequirement(NetworkRunner runner, PlayerRef player);
    public abstract string GetInfo(PlayerRef player);
}



public class BuildRequirementResult
{
    public bool Success => Reason == BuildFailReason.None;
    public BuildFailReason Reason;
    public string Message;

}


