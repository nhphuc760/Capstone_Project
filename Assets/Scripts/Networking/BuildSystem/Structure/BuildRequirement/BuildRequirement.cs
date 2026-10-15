using System;
using System.Collections.Generic;
using Fusion;

[Serializable]
public abstract class BuildRequirement
{
    public abstract void OnBuildExecute(NetworkRunner runner, PlayerRef playerRef);
    public abstract BuildRequirementResult CheckRequirement(NetworkRunner runner, PlayerRef player);
    public abstract string GetInfo(PlayerRef player);
}

[Serializable]
public abstract class EntityValueBuildRequirement: BuildRequirement
{   
    public List<EntityValue> values;
}

[Serializable]
public abstract class EventBuildRequirement : BuildRequirement 
{

}




public class BuildRequirementResult
{
    public bool Success => Reason == BuildFailReason.None;
    public BuildFailReason Reason;
    public string Message;

}


