using Fusion;
using UnityEngine;

public enum SessionState
    {
        Waiting,
        Ready,
        Running,
        Finished,
        Cancelled
    }

    public enum AuctionResult
    {
        None,
        Sold,
        NoBid,
        Tie,
        Cancelled,
        Failed
    }
public class TradeSession : NetworkBehaviour
{

    private const int NoBidValue = -1;

    [Networked]
    public SessionState State { get; private set; }

    [Networked]
    public AuctionResult Result { get; private set; }

    [Networked]
    public PlayerRef PlayerA { get; private set; }

    [Networked]
    public PlayerRef PlayerB { get; private set; }

    [Networked]
    public int ItemID { get; private set; }

    [Networked]
    public int ItemAmount { get; private set; }

    [Networked]
    public int PlayerABid { get; private set; }

    [Networked]
    public int PlayerBBid { get; private set; }

    [Networked]
    public PlayerRef Winner { get; private set; }

    [Networked]
    public int WinningBid { get; private set; }

    [Header("Trade")]
    [SerializeField]
    private ItemDatabase database;

    public override void Spawned()
    {
        base.Spawned();

        if (database == null)
        {
            database = Resources.Load<ItemDatabase>("ItemData");

            if (database == null)
            {
                Debug.LogError($"{name}: ItemDatabase not found.");
            }
        }
    }

    public bool Setup(PlayerRef playerA, PlayerRef playerB, int itemID, int itemAmount = 1)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (!playerA.IsRealPlayer || !playerB.IsRealPlayer || playerA == playerB)
            return false;

        if (itemAmount <= 0)
            return false;

        if (database == null || database.GetItem(itemID) == null)
            return false;

        PlayerA = playerA;
        PlayerB = playerB;
        ItemID = itemID;
        ItemAmount = itemAmount;
        PlayerABid = NoBidValue;
        PlayerBBid = NoBidValue;
        Winner = default;
        WinningBid = 0;
        Result = AuctionResult.None;
        State = SessionState.Ready;

        return true;
    }

    public bool StartAuction()
    {
        if (!Object.HasStateAuthority)
            return false;

        if (State != SessionState.Ready)
            return false;

        State = SessionState.Running;
        Result = AuctionResult.None;

        return true;
    }

    public bool PlaceBid(PlayerRef player, int bidAmount)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (State != SessionState.Running)
            return false;

        if (bidAmount <= 0)
            return false;

        if (player == PlayerA)
        {
            PlayerABid = bidAmount;
            return true;
        }

        if (player == PlayerB)
        {
            PlayerBBid = bidAmount;
            return true;
        }

        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlaceBid(int bidAmount, RpcInfo info = default)
    {
        PlaceBid(info.Source, bidAmount);
    }

    public bool FinishAuction()
    {
        if (!Object.HasStateAuthority)
            return false;

        if (State != SessionState.Running)
            return false;

        PlayerRef winner;
        int winningBid;

        if (!TryGetWinner(out winner, out winningBid))
        {
            State = SessionState.Finished;
            return false;
        }

        NetworkInventory inventory = GetInventory(winner);

        if (inventory == null)
        {
            Debug.LogWarning($"TRADE FAILED | Winner={winner} has no NetworkInventory.");
            Result = AuctionResult.Failed;
            State = SessionState.Finished;
            return false;
        }

        if (!inventory.Object.HasStateAuthority)
        {
            Debug.LogWarning($"TRADE FAILED | Winner={winner} inventory has no state authority.");
            Result = AuctionResult.Failed;
            State = SessionState.Finished;
            return false;
        }

        if (database == null || database.GetItem(ItemID) == null)
        {
            Debug.LogWarning($"TRADE FAILED | ItemID={ItemID} not found in TradeSession database.");
            Result = AuctionResult.Failed;
            State = SessionState.Finished;
            return false;
        }

        if (!inventory.AddItem(ItemID, ItemAmount))
        {
            Debug.LogWarning(
                $"TRADE FAILED | Could not add Item={ItemID} x{ItemAmount} to Winner={winner} inventory."
            );
            Result = AuctionResult.Failed;
            State = SessionState.Finished;
            return false;
        }

        Winner = winner;
        WinningBid = winningBid;
        Result = AuctionResult.Sold;
        State = SessionState.Finished;

        Debug.Log(
            $"TRADE SOLD | Winner={winner} | Item={ItemID} x{ItemAmount} | Bid={winningBid}"
        );

        return true;
    }

    public bool Cancel()
    {
        if (!Object.HasStateAuthority)
            return false;

        if (State == SessionState.Finished ||
            State == SessionState.Cancelled)
        {
            return false;
        }

        Result = AuctionResult.Cancelled;
        State = SessionState.Cancelled;

        return true;
    }

    private bool TryGetWinner(out PlayerRef winner, out int winningBid)
    {
        winner = default;
        winningBid = 0;

        bool playerAHasBid = PlayerABid != NoBidValue;
        bool playerBHasBid = PlayerBBid != NoBidValue;

        if (!playerAHasBid && !playerBHasBid)
        {
            Result = AuctionResult.NoBid;
            return false;
        }

        if (PlayerABid == PlayerBBid)
        {
            Result = AuctionResult.Tie;
            return false;
        }

        if (PlayerABid > PlayerBBid)
        {
            winner = PlayerA;
            winningBid = PlayerABid;
            return true;
        }

        winner = PlayerB;
        winningBid = PlayerBBid;
        return true;
    }

    private NetworkInventory GetInventory(PlayerRef player)
    {
        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        {
            return null;
        }

        return playerObject.GetComponent<NetworkInventory>();
    }
}
