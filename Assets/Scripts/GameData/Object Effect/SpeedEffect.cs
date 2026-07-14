using UnityEngine;

[CreateAssetMenu(menuName = "Objects Data/Object Effects/Speed Boost")]
public class SpeedBoostEffectData : ObjectEffectData
{
    public float speedMultiplier = 2f;
    public float duration = 5f;

    public override void Apply(GameObject target)
    {
        // PlayerMovement movement = target.GetComponent<PlayerMovement>();

        // if (movement != null)
        // {
        //     movement.StartSpeedBoost(speedMultiplier, duration);
        // }
    }
}