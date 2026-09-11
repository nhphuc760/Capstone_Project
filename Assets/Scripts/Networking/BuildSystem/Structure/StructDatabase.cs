using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "StructDatabase", menuName = "ScriptableObjects/StructDatabase")]
public class StructDatabase : ScriptableObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Dictionary<string, StructureDataSO> structDatabase = new Dictionary<string, StructureDataSO>();
    [SerializeField] List<StructureDataSO> structsList = new List<StructureDataSO>();



    [Title("Make sure StructDataSO inside Resources folder")]
    [Button]
    public void BakeStructData()
    {
        structsList = Resources.LoadAll<StructureDataSO>("ScriptableObjects/StructData").ToList();
        Initialize();
    }

    void Initialize()
    {      
        structDatabase = new Dictionary<string, StructureDataSO>();
        foreach (var i in structsList)
        {
            if (i == null) continue;
            if (structDatabase.ContainsKey(i._id.Value)) continue;
            structDatabase.Add(i._id.Value, i);
        }
    }

    public StructureDataSO GetStructSO(string _id)
    {
        if (structDatabase == null || structDatabase.Count == 0 )
        {
            Initialize();
        }

        if (structDatabase.TryGetValue(_id, out var value))
        {
            return value;
        }
        else
        {
            Debug.Log($"StructDatabase not contains {_id}");
            return null;
        }
    }

}
