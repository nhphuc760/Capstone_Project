using System;
using Fusion;
using UnityEngine;

[Serializable]
public struct StructureCost
{
    public int Wood;
    public int ironOre;
    public int copperOre;
    public int goldOre;
}

public abstract class StructureBase : NetworkBehaviour
{
    public StructureDataSO StructureDataSO;
    public abstract void Operation();


}
