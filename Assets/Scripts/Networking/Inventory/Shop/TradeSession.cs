using Fusion;
using UnityEngine;
using System.Linq;

public enum TradeSessionState
{
    None,
    Active,
    Completed,
    Cancelled
}

public class TradeSession : NetworkBehaviour
{
    [Networked] public TradeSessionState State { get; private set; }
    [Networked] public int ItemID { get; private set; }
    [Networked] public int ItemAmount { get; private set; }
    [Networked] public int CurrentBid { get; private set; }

    [Networked] public PlayerRef HighestBidder { get; private set; }
    [Networked] public PlayerRef Player1 { get; private set; }
    [Networked] public PlayerRef Player2 { get; private set; }

    public bool IsActive => State == TradeSessionState.Active;
    public bool IsCompleted => State == TradeSessionState.Completed;
    public bool IsCancelled => State == TradeSessionState.Cancelled;

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority)
            return;

        State = TradeSessionState.None;
        ItemID = 0;
        ItemAmount = 0;
        CurrentBid = 0;
        HighestBidder = PlayerRef.None;
        Player1 = PlayerRef.None;
        Player2 = PlayerRef.None;

    }

    #region Start Session
    public void StartSession(PlayerRef player1, PlayerRef player2, int itemID, int itemAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        #region Debug checks
        if (player1 == PlayerRef.None || player2 == PlayerRef.None)
        {
            return;
        }

        if (player1 == player2)
        {
            return;
        }

        if (itemID <= 0)
        {
            return;
        }

        if (itemAmount <= 0)
        {
            return;
        }
        #endregion Debug checks

        Player1 = player1;
        Player2 = player2;
        ItemID = itemID;
        ItemAmount = itemAmount;
        //CurrentBid = startingBid;
        HighestBidder = PlayerRef.None;
        State = TradeSessionState.Active;

        Debug.Log($"[TradeSession] Session Started | Player1={Player1} | Player2={Player2} | ItemID={ItemID} | Amount={ItemAmount} | StartingBid={CurrentBid}");
    }
    #endregion Start Session

    #region Place Bid
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlaceBid(int bidAmount, RpcInfo info = default)
    {
        PlayerRef bidder = info.Source;

        if (bidder == PlayerRef.None)
        {
            bidder = Runner.LocalPlayer;
        }

        ProcessBid(bidder, bidAmount);
    }

    private void ProcessBid(PlayerRef bidder, int bidAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        #region Debug checks
        if (State != TradeSessionState.Active)
        {
            return;
        }

        if (!IsParticipant(bidder))
        {
            return;
        }
        #endregion

        NetworkMoney bidderMoney = GetMoneyForPlayer(bidder);

        #region Money checks
        if (bidderMoney == null)
        {
            return;
        }

        if (!bidderMoney.HasMoney(bidAmount))
        {
            return;
        }

        if (bidAmount < 10)
        {
            return;
        }

        if (bidAmount <= CurrentBid)
        {
            return;
        }
        #endregion Money checks

        CurrentBid = bidAmount;
        HighestBidder = bidder;

        Debug.Log($"[TradeSession] Bid Accepted | HighestBidder={HighestBidder} | Player1={Player1} | Player2={Player2} | HighestBid={CurrentBid}");
    }

    public bool CanBid(int bidAmount)
    {
        if (State != TradeSessionState.Active)
            return false;

        if (bidAmount < 10)
            return false;

        return bidAmount > CurrentBid;
    }
    #endregion Place Bid

    #region player checks
    private bool IsParticipant(PlayerRef player)
    {
        return player == Player1 || player == Player2;
    }

    private NetworkMoney GetMoneyForPlayer(PlayerRef player)
    {
        foreach (NetworkMoney money in FindObjectsByType<NetworkMoney>(FindObjectsSortMode.None))
        {
            if (money.Object != null && money.Object.InputAuthority == player)
                return money;
        }

        return null;
    }

    private NetworkInventory GetInventoryForPlayer(PlayerRef player)
    {
        foreach (NetworkInventory inventory in FindObjectsByType<NetworkInventory>(FindObjectsSortMode.None))
        {
            if (inventory.Object != null && inventory.Object.InputAuthority == player)
                return inventory;
        }

        return null;
    }
    #endregion player checks

    #region Session Complete
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteSession(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        ProcessCompleteSession(sender);
    }

    private void ProcessCompleteSession(PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        #region Debug checks
        if (State != TradeSessionState.Active)
        {
            return;
        }

        if (!IsParticipant(sender))
        {
            return;
        }

        if (HighestBidder == PlayerRef.None)
        {
            return;
        }
        #endregion

        PlayerRef winner = GetWinningPlayer();

        if (winner == PlayerRef.None)
        {
            return;
        }

        NetworkMoney winnerMoney = GetMoneyForPlayer(winner);
        NetworkInventory winnerInventory = GetInventoryForPlayer(winner);

        if (winnerMoney == null || winnerInventory == null)
            return;

        // Validate both sides before modifying state.
        if (!winnerMoney.HasMoney(CurrentBid) || !winnerInventory.CanAddItem(ItemID, ItemAmount))
            return;

        if (!winnerInventory.AddItem(ItemID, ItemAmount))
            return;

        // Roll back the item if the money update unexpectedly fails.
        if (!winnerMoney.RemoveMoney(CurrentBid))
        {
            winnerInventory.RemoveItem(ItemID, ItemAmount);
            return;
        }

        State = TradeSessionState.Completed;

        Debug.Log(
            $"[TradeSession] Completed | " +
            $"Winner={winner} | " +
            $"FinalBid={CurrentBid}"
        );
    }

    public PlayerRef GetWinningPlayer()
    {
        return HighestBidder;
    }
    #endregion Session Complete

    #region Cancel Session
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CancelSession(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        ProcessCancelSession(sender);
    }

    private void ProcessCancelSession(PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != TradeSessionState.Active)
        {
            return;
        }

        if (!IsParticipant(sender))
        {
            return;
        }

        State = TradeSessionState.Cancelled;

        Debug.Log(
            $"[TradeSession] Cancelled | " +
            $"CancelledBy={sender}"
        );
    }
    #endregion Cancel Session

    #region Reset Session
    public void ResetSession()
    {
        if (!Object.HasStateAuthority)
            return;

        State = TradeSessionState.None;
        ItemID = 0;
        ItemAmount = 0;
        CurrentBid = 0;
        HighestBidder = PlayerRef.None;
        Player1 = PlayerRef.None;
        Player2 = PlayerRef.None;

    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestStartTestSession(RpcInfo info = default)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        PlayerRef[] players = Runner.ActivePlayers.ToArray();

        if (players.Length < 2)
        {
            return;
        }

        PlayerRef player1 = players[0];
        PlayerRef player2 = players[1];

        StartSession(player1, player2, itemID: 1, itemAmount: 1);
    }
    #endregion
}
