using Fusion;
using UnityEngine;


public enum StructureType
{
    None,
    Wall,
    Door,
    Turret,
    ElectricTower,
    Acceleration,
    BuffRangeTower,
    Refrigrator,
    Trap,
}

public enum StructureCategory
{
    None,
    Attack,//Tower, Turret, Trap
    Defense,//Shield, Wall, Door
    Support,//Buff, Heal
}



[CreateAssetMenu(fileName = "NewStructureData", menuName = "ScriptableObjects/StructureSO")]
public class StructureDataSO : ScriptableObject
{
    public Sprite icon;
    [Tooltip("NetworkString<_8>")]
    public NetworkString<_8> _id;
    public string _name;
    public string _description;
    public StructureType structureType;
    public StructureCategory structureCategory;
    //Build Requirement
    [SerializeReference]
    public RequirementDefinition[] buildRequirement;
    public GameObject prefabs;
    //Upgrade Requirement
    [SerializeReference]
    public StructLevelData[] levels;
 }


