using Fusion;
using UnityEngine;

public enum TradeSessionState
{
    None,
    WaitingForPlayers,
    Active,
    Completed,
    Cancelled
}

public class TradeSession : NetworkBehaviour
{
    private const float BidTimeout = 7f;

    [Header("Session Settings")]
    [SerializeField] private int startingBid = 0;

    [Networked] public TradeSessionState State { get; private set; }
    [Networked] public PlayerRef Player1 { get; private set; }
    [Networked] public PlayerRef Player2 { get; private set; }
    [Networked] public PlayerRef CurrentBidder { get; private set; }
    [Networked] public PlayerRef Winner { get; private set; }
    [Networked] public int CurrentBid { get; private set; }
    [Networked] public TickTimer BidTimer { get; private set; }

    public bool IsActive => State == TradeSessionState.Active;
    public bool HasTwoPlayers => Player1 != PlayerRef.None && Player2 != PlayerRef.None;
    public bool HasWinner => Winner != PlayerRef.None;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            State = TradeSessionState.None;
            Player1 = PlayerRef.None;
            Player2 = PlayerRef.None;
            CurrentBidder = PlayerRef.None;
            Winner = PlayerRef.None;
            CurrentBid = startingBid;
            BidTimer = TickTimer.None;
        }

        Debug.Log($"[TradeSession] Spawned | State={State}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || State != TradeSessionState.Active)
            return;

        if (!BidTimer.Expired(Runner))
            return;

        if (CurrentBidder != PlayerRef.None)
        {
            CompleteSession(CurrentBidder, "Bid timeout");
        }
        else
        {
            CancelSessionInternal("Timer expired without a valid bid");
        }
    }

    #region Start Session
    // Call this when the player presses Start Session / Join Session.
    public void StartSession()
    {
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[TradeSession] Start failed | Invalid NetworkObject");
            return;
        }

        RPC_RequestJoinSession();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestJoinSession(RpcInfo info = default)
    {
        PlayerRef requester = info.Source;

        Debug.Log(
            $"[TradeSession] Join Request | Requester={requester} | " +
            $"Player1={Player1} | Player2={Player2} | State={State}"
        );

        if (requester == PlayerRef.None)
        {
            Debug.LogWarning("[TradeSession] Join rejected | Invalid PlayerRef");
            return;
        }

        if (State == TradeSessionState.Active ||
            State == TradeSessionState.Completed ||
            State == TradeSessionState.Cancelled)
        {
            Debug.LogWarning($"[TradeSession] Join rejected | State={State}");
            return;
        }

        if (requester == Player1 || requester == Player2)
        {
            Debug.Log($"[TradeSession] Player already joined | Player={requester}");
            return;
        }

        if (Player1 == PlayerRef.None)
        {
            Player1 = requester;
            State = TradeSessionState.WaitingForPlayers;

            Debug.Log($"[TradeSession] Player 1 joined | Player1={Player1}");
            return;
        }

        if (Player2 == PlayerRef.None)
        {
            Player2 = requester;

            if (Player1 == Player2)
            {
                Player2 = PlayerRef.None;
                Debug.LogWarning("[TradeSession] Join rejected | Duplicate players");
                return;
            }

            StartSessionInternal();
            return;
        }

        Debug.LogWarning($"[TradeSession] Join rejected | Session full | Player={requester}");
    }

    private void StartSessionInternal()
    {
        if (!Object.HasStateAuthority || !HasTwoPlayers)
            return;

        State = TradeSessionState.Active;
        CurrentBid = startingBid;
        CurrentBidder = PlayerRef.None;
        Winner = PlayerRef.None;
        BidTimer = TickTimer.None;

        Debug.Log($"[TradeSession] Started | Player1={Player1} | Player2={Player2}");
    }
    #endregion

    #region Bid Management
    // Call this from the bid button.
    public void PlaceBid(int bidAmount)
    {
        if (Object == null || !Object.IsValid)
            return;

        RPC_RequestPlaceBid(bidAmount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPlaceBid(int bidAmount, RpcInfo info = default)
    {
        PlayerRef requester = info.Source;

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning("[TradeSession] Bid rejected | Session is not active");
            return;
        }

        if (!IsParticipant(requester))
        {
            Debug.LogWarning($"[TradeSession] Bid rejected | Not a participant | Player={requester}");
            return;
        }

        if (requester == CurrentBidder)
        {
            Debug.LogWarning($"[TradeSession] Bid rejected | Waiting for opponent | Player={requester}");
            return;
        }

        if (bidAmount <= CurrentBid)
        {
            Debug.LogWarning($"[TradeSession] Bid rejected | Amount={bidAmount} | CurrentBid={CurrentBid}");
            return;
        }

        CurrentBid = bidAmount;
        CurrentBidder = requester;
        BidTimer = TickTimer.CreateFromSeconds(Runner, BidTimeout);

        Debug.Log($"[TradeSession] Bid placed | Player={requester} | Bid={CurrentBid} | Timer={BidTimeout}s");
    }
    #endregion

    #region Bid Removal
    // A player can withdraw their own current bid.
    public void RemoveBid()
    {
        if (Object == null || !Object.IsValid)
            return;

        RPC_RequestRemoveBid();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRemoveBid(RpcInfo info = default)
    {
        PlayerRef requester = info.Source;

        if (State != TradeSessionState.Active)
        {
            Debug.LogWarning("[TradeSession] Remove bid rejected | Session is not active");
            return;
        }

        if (!IsParticipant(requester))
        {
            Debug.LogWarning($"[TradeSession] Remove bid rejected | Not a participant | Player={requester}");
            return;
        }

        if (requester != CurrentBidder)
        {
            Debug.LogWarning($"[TradeSession] Remove bid rejected | No active bid owned by Player={requester}");
            return;
        }

        CurrentBid = startingBid;
        CurrentBidder = PlayerRef.None;

        Debug.Log($"[TradeSession] Bid removed | Player={requester} | CurrentBid={CurrentBid}");
    }
    #endregion

    #region Quit Session
    // Call this before the player voluntarily leaves the auction.
    public void QuitSession()
    {
        if (Object == null || !Object.IsValid)
            return;

        RPC_RequestQuitSession();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestQuitSession(RpcInfo info = default)
    {
        PlayerRef quitter = info.Source;

        if (State != TradeSessionState.Active &&
            State != TradeSessionState.WaitingForPlayers)
        {
            Debug.LogWarning("[TradeSession] Quit ignored | Session already ended");
            return;
        }

        if (!IsParticipant(quitter))
        {
            Debug.LogWarning($"[TradeSession] Quit rejected | Not a participant | Player={quitter}");
            return;
        }

        PlayerRef opponent = GetOpponent(quitter);

        if (opponent != PlayerRef.None)
        {
            CompleteSession(opponent, $"Opponent quit: {quitter}");
        }
        else
        {
            CancelSessionInternal($"Player quit before opponent joined: {quitter}");
        }
    }
    #endregion

    #region Session Completion
    private void CompleteSession(PlayerRef winner, string reason)
    {
        if (!Object.HasStateAuthority || State != TradeSessionState.Active)
            return;

        Winner = winner;
        State = TradeSessionState.Completed;
        BidTimer = TickTimer.None;

        Debug.Log($"[TradeSession] Completed | Winner={Winner} | FinalBid={CurrentBid} | Reason={reason}");
    }

    private void CancelSessionInternal(string reason)
    {
        if (!Object.HasStateAuthority)
            return;

        State = TradeSessionState.Cancelled;
        Winner = PlayerRef.None;
        BidTimer = TickTimer.None;

        Debug.Log($"[TradeSession] Cancelled | Reason={reason}");
    }
    #endregion

    #region Player Checks
    private bool IsParticipant(PlayerRef player)
    {
        return player != PlayerRef.None && (player == Player1 || player == Player2);
    }

    private PlayerRef GetOpponent(PlayerRef player)
    {
        if (player == Player1) return Player2;

        if (player == Player2) return Player1;

        return PlayerRef.None;
    }

    public float GetRemainingTime()
    {
        if (!IsActive || !BidTimer.IsRunning) return 0f;

        return BidTimer.RemainingTime(Runner) ?? 0f;
    }
    #endregion
}