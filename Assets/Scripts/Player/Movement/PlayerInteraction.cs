using Fusion;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Physics Grabbing Setup")]
    [SerializeField] private Camera fpsCamera; 
    [SerializeField] private Transform holdPoint; 
    [SerializeField] private LayerMask grabLayer;
    [SerializeField] private float grabDistance = 4f;
    [SerializeField] private float pullForce = 15f;

    [Networked] private NetworkObject heldObject { get; set; }

    private void Update()
    {
        if (!HasInputAuthority) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null) TryGrabLocal();
        }
        if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.E))
        {
            if (heldObject != null) RPC_DropItem();
        }
    }

    private void TryGrabLocal()
    {
        if (fpsCamera == null || !fpsCamera.gameObject.activeInHierarchy) return;

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, grabLayer))
        {
            NetworkObject targetObj = hit.collider.GetComponent<NetworkObject>();
            if (targetObj != null)
            {
                RPC_TryGrab(targetObj.Id);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TryGrab(NetworkId targetObjId)
    {
        if (Runner.TryFindObject(targetObjId, out NetworkObject targetObj))
        {
            heldObject = targetObj;

            Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.isKinematic = false;
                objRb.useGravity = false;
                objRb.linearDamping = 10f;
                objRb.angularDamping = 10f;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_DropItem()
    {
        if (heldObject != null)
        {
            Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.useGravity = true;
                objRb.linearDamping = 0f;         
                objRb.angularDamping = 0.05f;
            }

            heldObject = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (heldObject != null && Object.HasStateAuthority)
        {
            Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                Vector3 moveDirection = holdPoint.position - heldObject.transform.position;
                objRb.linearVelocity = moveDirection * pullForce;
            }
        }
    }
}