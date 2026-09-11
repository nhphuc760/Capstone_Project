using UnityEngine;
using Fusion;
using Sydewa;
public class DayNightFusionBridge : SimulationBehaviour
{
    [SerializeField] private NetworkTimeManager timeManager;
    [SerializeField] private LightingManager lightingManager;

    private void Update()
    {
        if (timeManager == null || lightingManager == null) return;
        if (timeManager.Object != null && timeManager.Object.IsValid)
        {
            lightingManager.TimeOfDay = timeManager.CurrentTimeOfDay;
        }
    }
}
