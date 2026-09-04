using System.Collections.Generic;
using Fusion;
using UnityEngine;

public  interface IBuildStategy
{
    
    BuildValidationResult CanBuild(NetworkRunner runner,PlayerRef playerRef , Vector3Int cell);
    void Build(NetworkRunner runner,PlayerRef playerRef, Vector3Int cell);   
}

public struct BuildValidationResult
{
    public bool Success => Reason == BuildFailReason.None;
    public BuildFailReason Reason { get; set; }

    public (ResourceType, int)[] missingResources;
    public string Message { get; set; }
    public static BuildValidationResult OK()
    {
        return new() { Reason = BuildFailReason.None };
    }
    public static BuildValidationResult Fail(BuildFailReason reason, string message = null)
    {
        return new() { Reason = reason, Message = message};
    }

    public static BuildValidationResult NotEnough((ResourceType, int)[] missingResource = null, string message = null)
    {        
        return new() {Reason = BuildFailReason.NotEnoughResource, missingResources = missingResource, Message = message };
    }

    
}


public enum BuildFailReason
{
    None,   
    DoorCompleted,
    OtherDoorInSafeZone,
    NotEnoughResource,
    InvalidPosition,
    OccupiedByStructure,
    MissingRequirement
}