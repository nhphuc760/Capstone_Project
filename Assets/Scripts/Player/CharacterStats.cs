using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "CharacterStats", menuName = "ScriptableObjects/CharacterStats")]
[DrawWithTriInspector]
public class CharacterStats : ScriptableObject
{
    public List<Stat> stats;
    private void OnEnable()
    {
        if(stats == null) stats = new List<Stat>();
     }
    //private void OnValidate()
    //{         
    //    var stat = stats.LastOrDefault();
    //    for (int i = stats.Count - 2; i >=0; i--)
    //    {
    //        if (stats[i].type == stat.type)
    //        {
    //            Debug.Log($"Has element type {stat.type} in list");
    //            stats.Remove(stat);
    //            return;
    //        }
    //    }
    //}
}


[Serializable]
public struct Stat
{
    public StatType type;
    public float value;
}
