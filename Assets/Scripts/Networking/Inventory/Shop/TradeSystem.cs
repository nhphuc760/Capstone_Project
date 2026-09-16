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

public class TradeSystem : NetworkBehaviour
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

        Debug.Log("[TradeSession] Spawned and initialized.");
    }

    public void StartSession(PlayerRef player1, PlayerRef player2, int itemID, int itemAmount)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.Log("[TradeSession] Start Session Failed | No State Authority");
            return;
        }

        #region Debug checks
        if (player1 == PlayerRef.None || player2 == PlayerRef.None)
        {
            Debug.Log($"[TradeSession] Start Session Failed | Invalid Players | Player1={player1} | Player2={player2}");
            return;
        }

        if (player1 == player2)
        {
            Debug.Log($"[TradeSession] Start Session Failed | Player1 and Player2 are the same | Player={player1}");
            return;
        }

        if (itemID <= 0)
        {
            Debug.Log($"[TradeSession] Start Session Failed | Invalid ItemID={itemID}");
            return;
        }

        if (itemAmount <= 0)
        {
            Debug.Log($"[TradeSession] Start Session Failed | Invalid ItemAmount={itemAmount}");
            return;
        }

        // if (startingBid < 10 || startingBid > 100)
        // {
        //     Debug.Log($"[TradeSession] Start Session Failed | Invalid StartingBid={startingBid} | ValidRange=10-100");
        //     return;
        // }
        #endregion

        Player1 = player1;
        Player2 = player2;
        ItemID = itemID;
        ItemAmount = itemAmount;
        //CurrentBid = startingBid;
        HighestBidder = PlayerRef.None;
        State = TradeSessionState.Active;

        Debug.Log($"[TradeSession] Session Started | Player1={Player1} | Player2={Player2} | ItemID={ItemID} | Amount={ItemAmount} | StartingBid={CurrentBid}");
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlaceBid(int bidAmount, RpcInfo info = default)
    {
        PlayerRef bidder = info.Source;

        if (bidder == PlayerRef.None)
        {
            bidder = Runner.LocalPlayer;
        }

        Debug.Log($"[TradeSession] Bid Request Received | Sender={info.Source} | ResolvedBidder={bidder} | Bid={bidAmount}");

        ProcessBid(bidder, bidAmount);
    }

    private void ProcessBid(PlayerRef bidder, int bidAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        #region Debug checks
        if (State != TradeSessionState.Active)
        {
            Debug.Log($"[TradeSession] Bid Rejected | Reason=SessionNotActive | Bidder={bidder} | Bid={bidAmount}");
            return;
        }

        if (!IsParticipant(bidder))
        {
            Debug.Log($"[TradeSession] Bid Rejected | Reason=NotParticipant | Bidder={bidder} | Player1={Player1} | Player2={Player2}");
            return;
        }

        if (bidAmount < 10 || bidAmount > 100)
        {
            Debug.Log($"[TradeSession] Bid Rejected | Reason=InvalidBid | Bidder={bidder} | Bid={bidAmount} | ValidRange=10-100");
            return;
        }

        if (bidAmount <= CurrentBid)
        {
            Debug.Log($"[TradeSession] Bid Rejected | Reason=BidNotHigher | Bidder={bidder} | Bid={bidAmount} | CurrentBid={CurrentBid}");
            return;
        }
        #endregion

        CurrentBid = bidAmount;
        HighestBidder = bidder;

        Debug.Log($"[TradeSession] Bid Accepted | HighestBidder={HighestBidder} | Player1={Player1} | Player2={Player2} | HighestBid={CurrentBid}");
    }

    private bool IsParticipant(PlayerRef player)
    {
        return player == Player1 || player == Player2;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteSession(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        Debug.Log(
            $"[TradeSession] Complete request received | " +
            $"Sender={sender}"
        );

        ProcessCompleteSession(sender);
    }

    private void ProcessCompleteSession(PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        #region Debug checks

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning(
                $"[TradeSession] Complete rejected. State={State}"
            );

            return;
        }

        if (!IsParticipant(sender))
        {
            Debug.LogWarning(
                $"[TradeSession] Complete rejected. " +
                $"Player {sender} is not a participant."
            );

            return;
        }

        if (HighestBidder == PlayerRef.None)
        {
            Debug.LogWarning(
                "[TradeSession] Cannot complete. " +
                "There is no valid bidder."
            );

            return;
        }

        #endregion

        PlayerRef winner = GetWinningPlayer();

        if (winner == PlayerRef.None)
        {
            Debug.LogError(
                "[TradeSession] Cannot complete. " +
                "Winning PlayerRef is invalid."
            );

            return;
        }

        State = TradeSessionState.Completed;

        Debug.Log(
            $"[TradeSession] Completed | " +
            $"Winner={winner} | " +
            $"FinalBid={CurrentBid}"
        );

        // TODO:
        // 1. Kiểm tra tiền của winner
        // 2. Trừ tiền winner
        // 3. Trao Item cho winner
        // 4. Xử lý item được đấu giá
        // 5. Lưu transaction
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CancelSession(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        Debug.Log(
            $"[TradeSession] Cancel request received | " +
            $"Sender={sender}"
        );

        ProcessCancelSession(sender);
    }

    private void ProcessCancelSession(PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning(
                $"[TradeSession] Cancel rejected. State={State}"
            );

            return;
        }

        if (!IsParticipant(sender))
        {
            Debug.LogWarning(
                $"[TradeSession] Cancel rejected. " +
                $"Player {sender} is not a participant."
            );

            return;
        }

        State = TradeSessionState.Cancelled;

        Debug.Log(
            $"[TradeSession] Cancelled | " +
            $"CancelledBy={sender}"
        );
    }

    public PlayerRef GetWinningPlayer()
    {
        return HighestBidder;
    }

    public bool CanBid(int bidAmount)
    {
        if (State != TradeSessionState.Active)
            return false;

        if (bidAmount < 10)
            return false;

        if (bidAmount > 100)
            return false;

        return bidAmount > CurrentBid;
    }

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

        Debug.Log("[TradeSession] Session reset.");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_RequestStartTestSession(RpcInfo info = default)
{
    Debug.Log($"[TradeSession] Start Session Request | Requester={info.Source}");

    if (!Object.HasStateAuthority)
    {
        Debug.Log("[TradeSession] Start Session Failed | Request received without State Authority");
        return;
    }

    PlayerRef[] players = Runner.ActivePlayers.ToArray();

    if (players.Length < 2)
    {
        Debug.Log($"[TradeSession] Start Session Failed | Need 2 players | CurrentPlayers={players.Length}");
        return;
    }

    PlayerRef player1 = players[0];
    PlayerRef player2 = players[1];

    Debug.Log($"[TradeSession] Players Found | Player1={player1} | Player2={player2}");

    StartSession(player1, player2, itemID: 1, itemAmount: 1);
}
}