using System.Collections.Generic;
using System.Linq;
using Fusion;
using TriInspector;
using UnityEngine;


public enum StructureType
{
    Attack,
    Defense,
    Buff
}




[CreateAssetMenu(fileName = "NewStructureData", menuName = "ScriptableObjects/StructureSO")]
public class StructureDataSO : ScriptableObject
{
    public Sprite icon;
    public NetworkString<_8> _id;
    public string _name;
    public string _description;
    public StructureType structureType;
    public StructureCost costBuild;
    public GameObject prefabs;
 }


