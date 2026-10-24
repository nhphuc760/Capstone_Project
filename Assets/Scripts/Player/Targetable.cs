
using UnityEngine;

public class Targetable : MonoBehaviour
{
    [field: SerializeField]
    public Transform AimPoint { get; private set; }

    [field: SerializeField]
    public bool CanBeTargeted { get; private set; } = true;

    [Header("Indicator Target")]

    [SerializeField]
    GameObject IndicatorTarget;
    [SerializeField]
    float sinA = .5f;
    [SerializeField]
    float freq = 1f;

    Vector3 originPos;


    public Transform TargetTransform =>
        AimPoint != null ? AimPoint : transform;


    private void Awake()
    {
        SetActiveIndicator(false);
        originPos = IndicatorTarget.transform.localPosition;
    }



    private void Update()
    {
        float x = sinA * Mathf.Sin(2 * Mathf.PI * freq * Time.time);
        if(IndicatorTarget)
        {
            IndicatorTarget.transform.localPosition = originPos + Vector3.up * x;
            LockAtPlayer();
        }
    }


    void LockAtPlayer()
    {
        var target = Camera.main.transform;
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // Chỉ xoay theo trục Y

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            IndicatorTarget.transform.rotation = targetRotation;
        }
    }

    public void SetActiveIndicator(bool value)
    {
        if (IndicatorTarget)
            IndicatorTarget.SetActive(value);
    }

}
