using UnityEngine;

[CreateAssetMenu(menuName = "Objects Data/Object Effects/Add Time")]
public class AddTimeEffectData : ObjectEffectData
{
    public float time = 30f;

    public override void Apply(GameObject target)
    {
        //TimeManager.Instance.AddTime(time);
    }
}