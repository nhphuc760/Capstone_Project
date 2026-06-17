using UnityEngine;

public class AbilityExcutor : MonoBehaviour
{
    [SerializeField] AbilityData abilityData;
    [SerializeField] GameObject target;
    public void Execute(GameObject target)
    {
        foreach (var effect in abilityData.effects)
        {
            effect.Execute(gameObject, target);
        }
    }
}
