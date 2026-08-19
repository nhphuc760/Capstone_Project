using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum Mode
{
    SharedInv,
    PersonalInv
}

public class InventoryMode : NetworkBehaviour
{
    [Header("Shared Inventory")]
    [SerializeField]
    private SharedInventory sharedInventory;

    [Networked]
    public Mode mode { get; private set; }

    public SharedInventory SharedInventory =>
        sharedInventory;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            mode = Mode.SharedInv;
        }
    }

    // =====================================================
    // GET ACTIVE INVENTORY
    // =====================================================

    public NetworkInventory GetActiveInventory(
        NetworkObject playerObject)
    {
        if (mode == Mode.SharedInv)
        {
            return null;
        }

        return playerObject.GetComponent<NetworkInventory>();
    }

    // =====================================================
    // SPLIT
    // =====================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_SplitInventory()
    {
        if (mode != Mode.SharedInv)
            return;

        SplitSharedInventory();

        mode = Mode.PersonalInv;

        Debug.Log(
            "Inventory switched to PERSONAL."
        );
    }

    // =====================================================
    // TRANSFER SHARED → PERSONAL
    // =====================================================

    private void SplitSharedInventory()
    {
        if (!Object.HasStateAuthority)
            return;

        List<PlayerRef> players =
            new List<PlayerRef>();

        foreach (PlayerRef player
                 in Runner.ActivePlayers)
        {
            players.Add(player);
        }

        if (players.Count == 0)
            return;

        // ---------------------------------------------
        // LOOP SHARED INVENTORY
        // ---------------------------------------------

        for (
            int slotIndex = 0;
            slotIndex < sharedInventory.Capacity;
            slotIndex++)
        {
            SharedInventoryItem sharedItem =
                sharedInventory.GetSlot(slotIndex);

            if (sharedItem.IsEmpty)
                continue;

            // -----------------------------------------
            // FIND OWNER
            // -----------------------------------------

            if (!Runner.TryGetPlayerObject(
                    sharedItem.owner,
                    out NetworkObject playerObject))
            {
                Debug.LogError(
                    $"Cannot find PlayerObject " +
                    $"for owner {sharedItem.owner}"
                );

                continue;
            }

            NetworkInventory personalInventory =
                playerObject.GetComponent<NetworkInventory>();

            if (personalInventory == null)
            {
                Debug.LogError(
                    $"Player {sharedItem.owner} " +
                    $"has no NetworkInventory."
                );

                continue;
            }

            // -----------------------------------------
            // TRANSFER
            // -----------------------------------------

            bool success =
                personalInventory.AddItem(
                    sharedItem.itemID,
                    sharedItem.amount
                );

            if (!success)
            {
                Debug.LogError(
                    $"Failed to transfer " +
                    $"Item ID {sharedItem.itemID} " +
                    $"x{sharedItem.amount} " +
                    $"to {sharedItem.owner}"
                );

                continue;
            }

            Debug.Log(
                $"Transferred Item ID " +
                $"{sharedItem.itemID} x{sharedItem.amount} " +
                $"to {sharedItem.owner}"
            );

            // -----------------------------------------
            // REMOVE FROM SHARED
            // -----------------------------------------

            sharedInventory.ClearSlot(
                slotIndex
            );
        }
    }
}