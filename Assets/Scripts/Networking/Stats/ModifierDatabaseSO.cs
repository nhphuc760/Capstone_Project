using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "ModifierDatabase", menuName = "ScriptableObjects/ModifierDatabaseSO")]
public class ModifierDatabaseSO : ScriptableObject
{
    public List<ModifierDataSO> modifierDatabaseSO = new List<ModifierDataSO>();


    [Button]
    public void Bake()
    {
        modifierDatabaseSO = Resources.LoadAll<ModifierDataSO>("ScriptableObjects/ModifierData").ToList();
    }

    public ModifierDataSO GetModifierDataSOByID(string _id)
    {
        return modifierDatabaseSO.FirstOrDefault(x => x.ModID.Value == _id);
    }
}

