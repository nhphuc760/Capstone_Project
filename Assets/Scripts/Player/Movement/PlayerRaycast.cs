using UnityEngine;
using Fusion;

public class PlayerRaycast: NetworkBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera fpsCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("References")]
    [SerializeField] private PlayerAnimator playerAnimator;
    private InteractableItem currentTarget;

    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        PerformRaycast();
        HandleInput();
    }

    private void PerformRaycast()
    {
        if (fpsCamera == null) return;
        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
        {
            InteractableItem interactable = hit.collider.GetComponent<InteractableItem>();
            if (interactable != null)
            {
                if (interactable != currentTarget)
                {
                    ClearTarget();
                    currentTarget = interactable;
                    currentTarget.ToggleHighlight(true);
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }
    private void HandleInput()
    {
        if(currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            if (playerAnimator != null)
            {
                playerAnimator.RPC_PlayPickUpAnimation();
            }
            currentTarget.PickUpItem();
            ClearTarget();
        }
    }
    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.ToggleHighlight(false);
            currentTarget = null;
        }
    }
}
