
using UnityEngine;
using FischlWorks_FogWar;

public class MinimapFogVisibility : MonoBehaviour
{
    [Header("Fog Settings")]
    [SerializeField] private csFogWar fogWar;

    [SerializeField] private int additionalRadius = 0;

    [Header("Minimap Settings")]
    [SerializeField] private SpriteRenderer[] iconRenderers;

    private bool currentVisibility;

    private void Start()
    {
        if (fogWar == null)
        {
            fogWar = FindFirstObjectByType<csFogWar>();
        }

        if (iconRenderers == null || iconRenderers.Length == 0)
        {
            iconRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        UpdateVisibility();
    }

    private void Update()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (fogWar == null)
        {
            SetIconVisibility(false);
            return;
        }

        Vector3 worldPosition = transform.position;

        // Check whether the object is inside AOS grid
        if (!fogWar.CheckWorldGridRange(worldPosition))
        {
            SetIconVisibility(false);
            return;
        }

        // Check whether the object is currently visible
        bool isVisible = fogWar.CheckVisibility(
            worldPosition,
            additionalRadius
        );

        SetIconVisibility(isVisible);
    }

    private void SetIconVisibility(bool visible)
    {
        if (currentVisibility == visible)
            return;

        currentVisibility = visible;

        foreach (SpriteRenderer icon in iconRenderers)
        {
            if (icon != null)
            {
                icon.enabled = visible;
            }
        }
    }
}
