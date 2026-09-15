using Fusion;
using UnityEngine;

public enum TradeSessionState
{
    None,
    Active,
    Completed,
    Cancelled
}

public enum Bidder
{
    None,
    Player_1,
    Player_2
}

public class TradeSession : NetworkBehaviour
{
    // =========================================================
    // NETWORKED SESSION STATE
    // =========================================================

    [Networked]
    public TradeSessionState State { get; private set; }

    [Networked]
    public int ItemID { get; private set; }

    [Networked]
    public int ItemAmount { get; private set; }

    [Networked]
    public int CurrentBid { get; private set; }

    [Networked]
    public Bidder CurrentBidder { get; private set; }


    // =========================================================
    // NETWORKED PLAYERS
    // =========================================================

    [Networked]
    public PlayerRef Player1 { get; private set; }

    [Networked]
    public PlayerRef Player2 { get; private set; }


    // =========================================================
    // LOCAL HELPERS
    // =========================================================

    public bool IsActive =>
        State == TradeSessionState.Active;

    public bool IsCompleted =>
        State == TradeSessionState.Completed;

    public bool IsCancelled =>
        State == TradeSessionState.Cancelled;


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority)
            return;

        State = TradeSessionState.None;

        ItemID = 0;
        ItemAmount = 0;

        CurrentBid = 0;
        CurrentBidder = Bidder.None;

        Player1 = PlayerRef.None;
        Player2 = PlayerRef.None;

        Debug.Log(
            "[TradeSession] Spawned and initialized."
        );
    }


    // =========================================================
    // START SESSION
    // =========================================================

    public void StartSession(
        PlayerRef player1,
        PlayerRef player2,
        int itemID,
        int itemAmount,
        int startingBid)
    {
        if (!Object.HasStateAuthority)
            return;

        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (State != TradeSessionState.None)
        {
            Debug.LogWarning(
                "[TradeSession] Cannot start session. " +
                $"Current State={State}"
            );

            return;
        }

        if (player1 == PlayerRef.None)
        {
            Debug.LogWarning(
                "[TradeSession] Player1 is invalid."
            );

            return;
        }

        if (player2 == PlayerRef.None)
        {
            Debug.LogWarning(
                "[TradeSession] Player2 is invalid."
            );

            return;
        }

        if (player1 == player2)
        {
            Debug.LogWarning(
                "[TradeSession] Player1 and Player2 " +
                "cannot be the same."
            );

            return;
        }

        if (itemID <= 0)
        {
            Debug.LogWarning(
                "[TradeSession] Invalid ItemID."
            );

            return;
        }

        if (itemAmount <= 0)
        {
            Debug.LogWarning(
                "[TradeSession] Invalid ItemAmount."
            );

            return;
        }

        if (startingBid < 0)
        {
            Debug.LogWarning(
                "[TradeSession] Starting bid cannot be negative."
            );

            return;
        }

        // -----------------------------------------------------
        // SET SESSION DATA
        // -----------------------------------------------------

        Player1 = player1;
        Player2 = player2;

        ItemID = itemID;
        ItemAmount = itemAmount;

        CurrentBid = startingBid;
        CurrentBidder = Bidder.None;

        State = TradeSessionState.Active;

        Debug.Log(
            $"[TradeSession] Started | " +
            $"Player1={Player1} | " +
            $"Player2={Player2} | " +
            $"ItemID={ItemID} | " +
            $"Amount={ItemAmount} | " +
            $"StartingBid={CurrentBid}"
        );
    }


    // =========================================================
    // RPC - PLAYER REQUEST BID
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_PlaceBid(
        int bidAmount,
        RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        Debug.Log(
            $"[TradeSession] Bid request received | " +
            $"Sender={sender} | " +
            $"Bid={bidAmount}"
        );

        ProcessBid(
            sender,
            bidAmount
        );
    }


    // =========================================================
    // PROCESS BID
    // =========================================================

    private void ProcessBid(
        PlayerRef sender,
        int bidAmount)
    {
        if (!Object.HasStateAuthority)
            return;

        // -----------------------------------------------------
        // CHECK SESSION
        // -----------------------------------------------------

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning(
                "[TradeSession] Bid rejected. " +
                $"Session State={State}"
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK PARTICIPANT
        // -----------------------------------------------------

        if (!IsParticipant(sender))
        {
            Debug.LogWarning(
                $"[TradeSession] Bid rejected. " +
                $"Player {sender} is not a participant."
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK BID RANGE
        // -----------------------------------------------------

        if (bidAmount < 10 || bidAmount > 100)
        {
            Debug.LogWarning(
                $"[TradeSession] Bid rejected. " +
                $"Bid must be between 10 and 100. " +
                $"Requested={bidAmount}"
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK HIGHER THAN CURRENT BID
        // -----------------------------------------------------

        if (bidAmount <= CurrentBid)
        {
            Debug.LogWarning(
                $"[TradeSession] Bid rejected. " +
                $"CurrentBid={CurrentBid} | " +
                $"RequestedBid={bidAmount}"
            );

            return;
        }

        // -----------------------------------------------------
        // ACCEPT BID
        // -----------------------------------------------------

        CurrentBid = bidAmount;

        CurrentBidder = GetBidder(sender);

        Debug.Log(
            $"[TradeSession] Bid accepted | " +
            $"Bidder={CurrentBidder} | " +
            $"Player={sender} | " +
            $"Bid={CurrentBid}"
        );
    }


    // =========================================================
    // CHECK PARTICIPANT
    // =========================================================

    private bool IsParticipant(PlayerRef player)
    {
        return player == Player1 ||
               player == Player2;
    }


    // =========================================================
    // GET BIDDER
    // =========================================================

    private Bidder GetBidder(PlayerRef player)
    {
        if (player == Player1)
            return Bidder.Player_1;

        if (player == Player2)
            return Bidder.Player_2;

        return Bidder.None;
    }


    // =========================================================
    // COMPLETE SESSION - RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_CompleteSession(
        RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        Debug.Log(
            $"[TradeSession] Complete request received | " +
            $"Sender={sender}"
        );

        ProcessCompleteSession(sender);
    }


    // =========================================================
    // PROCESS COMPLETE
    // =========================================================

    private void ProcessCompleteSession(
        PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        // -----------------------------------------------------
        // CHECK SESSION
        // -----------------------------------------------------

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning(
                "[TradeSession] Complete rejected. " +
                $"Session State={State}"
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK PARTICIPANT
        // -----------------------------------------------------

        if (!IsParticipant(sender))
        {
            Debug.LogWarning(
                $"[TradeSession] Complete rejected. " +
                $"Player {sender} is not a participant."
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK BIDDER
        // -----------------------------------------------------

        if (CurrentBidder == Bidder.None)
        {
            Debug.LogWarning(
                "[TradeSession] Cannot complete. " +
                "There is no valid bidder."
            );

            return;
        }

        // -----------------------------------------------------
        // GET WINNER
        // -----------------------------------------------------

        PlayerRef winner = GetWinningPlayer();

        if (winner == PlayerRef.None)
        {
            Debug.LogError(
                "[TradeSession] Cannot complete. " +
                "Winning PlayerRef is invalid."
            );

            return;
        }

        // -----------------------------------------------------
        // COMPLETE
        // -----------------------------------------------------

        State = TradeSessionState.Completed;

        Debug.Log(
            $"[TradeSession] Completed | " +
            $"Winner={CurrentBidder} | " +
            $"WinningPlayer={winner} | " +
            $"FinalBid={CurrentBid}"
        );

        // TODO:
        // 1. Kiểm tra tiền của winner
        // 2. Trừ tiền winner
        // 3. Trao Item cho winner
        // 4. Xử lý item được đấu giá
        // 5. Lưu transaction
    }


    // =========================================================
    // CANCEL SESSION - RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_CancelSession(
        RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        Debug.Log(
            $"[TradeSession] Cancel request received | " +
            $"Sender={sender}"
        );

        ProcessCancelSession(sender);
    }


    // =========================================================
    // PROCESS CANCEL
    // =========================================================

    private void ProcessCancelSession(
        PlayerRef sender)
    {
        if (!Object.HasStateAuthority)
            return;

        // -----------------------------------------------------
        // CHECK SESSION
        // -----------------------------------------------------

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning(
                "[TradeSession] Cancel rejected. " +
                $"Session State={State}"
            );

            return;
        }

        // -----------------------------------------------------
        // CHECK PARTICIPANT
        // -----------------------------------------------------

        if (!IsParticipant(sender))
        {
            Debug.LogWarning(
                $"[TradeSession] Cancel rejected. " +
                $"Player {sender} is not a participant."
            );

            return;
        }

        // -----------------------------------------------------
        // CANCEL
        // -----------------------------------------------------

        State = TradeSessionState.Cancelled;

        Debug.Log(
            $"[TradeSession] Cancelled | " +
            $"CancelledBy={sender}"
        );
    }


    // =========================================================
    // GET WINNING PLAYER
    // =========================================================

    public PlayerRef GetWinningPlayer()
    {
        if (CurrentBidder == Bidder.Player_1)
            return Player1;

        if (CurrentBidder == Bidder.Player_2)
            return Player2;

        return PlayerRef.None;
    }


    // =========================================================
    // GET PLAYER BIDDER
    // =========================================================

    public Bidder GetPlayerBidder(
        PlayerRef player)
    {
        return GetBidder(player);
    }


    // =========================================================
    // CHECK BID
    // =========================================================

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


    // =========================================================
    // RESET SESSION
    // =========================================================

    public void ResetSession()
    {
        if (!Object.HasStateAuthority)
            return;

        State = TradeSessionState.None;

        ItemID = 0;
        ItemAmount = 0;

        CurrentBid = 0;
        CurrentBidder = Bidder.None;

        Player1 = PlayerRef.None;
        Player2 = PlayerRef.None;

        Debug.Log(
            "[TradeSession] Session reset."
        );
    }


    // =========================================================
    // RPC - REQUEST START TEST SESSION
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    public void RPC_RequestStartTestSession(
        RpcInfo info = default)
    {
        PlayerRef requester = info.Source;

        Debug.Log(
            $"[TradeSession] Start Session Request | " +
            $"Requester={requester}"
        );

        if (!Object.HasStateAuthority)
            return;

        if (State != TradeSessionState.None)
        {
            Debug.LogWarning(
                "[TradeSession] Cannot start test. " +
                $"Session State={State}"
            );

            return;
        }

        if (Runner == null)
        {
            Debug.LogError(
                "[TradeSession] Runner is null."
            );

            return;
        }

        PlayerRef player1 = PlayerRef.None;
        PlayerRef player2 = PlayerRef.None;

        // -----------------------------------------------------
        // FIND FIRST TWO PLAYERS
        // -----------------------------------------------------

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player1 == PlayerRef.None)
            {
                player1 = player;
            }
            else if (player2 == PlayerRef.None)
            {
                player2 = player;
                break;
            }
        }

        // -----------------------------------------------------
        // CHECK TWO PLAYERS
        // -----------------------------------------------------

        if (player1 == PlayerRef.None ||
            player2 == PlayerRef.None)
        {
            Debug.LogWarning(
                "[TradeSession] Need at least 2 players."
            );

            return;
        }

        // -----------------------------------------------------
        // START TEST SESSION
        // -----------------------------------------------------

        StartSession(
            player1,
            player2,
            itemID: 1,
            itemAmount: 1,
            startingBid: 10
        );
    }
}