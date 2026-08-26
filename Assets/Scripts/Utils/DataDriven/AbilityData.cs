using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityData", menuName = "ScriptableObjects/AbilityData")]
class AbilityData : ScriptableObject
{
    public string label;
    [SerializeReference] public List<AbilityEffect> effects;
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(label)) label = name;
        if(effects == null) effects = new List<AbilityEffect>();

    }
}

[Serializable]
public abstract class AbilityEffect     
{
    public abstract void Execute(GameObject caster, GameObject target);
}
