using UnityEngine;
using Fusion;
public class PlayerDropItem : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Camera fpsCamera; 
    [SerializeField] private PlayerAnimator playerAnimator; 

    [Header("Drop Settings")]
    [SerializeField] private float dropForwardOffset = 1.2f; 
    [SerializeField] private float dropForce = 5f;


    private void Update()
    {
        //Đoạn này dùng để test nhanh, sau này có Balo thì sẽ xóa đi và gọi từ UI Balo
        if (HasInputAuthority && Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Bấm Q để quăng đồ, chờ nối code Balo");
        }
    }

    // Hàm này sẽ được gọi trực tiếp từ kho đồ (Inventory) sau này
    public void DropItemFromInventory(NetworkPrefabRef itemPrefabToDrop)
    {
        if (!HasInputAuthority) return;
        if (playerAnimator != null)
        {
            playerAnimator.RPC_PlayDropAnimation();
        }
        RPC_RequestDropItem(itemPrefabToDrop, fpsCamera.transform.position, fpsCamera.transform.forward);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDropItem(NetworkPrefabRef prefabRef, Vector3 origin, Vector3 direction)
    {
        Vector3 spawnPosition = origin + (direction * dropForwardOffset);
        NetworkObject droppedItem = Runner.Spawn(prefabRef, spawnPosition, Quaternion.identity, Object.StateAuthority);
        if (droppedItem != null)
        {
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction * dropForce, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * dropForce);
            }
        }
    }
}
