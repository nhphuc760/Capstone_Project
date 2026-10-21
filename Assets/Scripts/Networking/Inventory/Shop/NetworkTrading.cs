using System.Linq;
using Fusion;
using UnityEngine;

public enum AuctionState
{
    None,
    Active,
    Completed,
    Cancelled
}

public class NetworkTrading : NetworkBehaviour
{
    private const int MaxPlayers = 4;

    [Header("Auction Settings")]
    [SerializeField] private ItemSO itemSO;
    [SerializeField] private float auctionDuration = 10f;

    [Header("Participants")]
    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<PlayerRef> Participants => default;

    [Networked]
    public int ParticipantCount { get; private set; }

    [Header("Auction Data")]
    [Networked] public int ItemID { get; private set; }
    [Networked] public int Quantity { get; private set; }
    [Networked] public int StartingPrice { get; private set; }
    [Networked] public int CurrentBid { get; private set; }

    [Networked] public PlayerRef HighestBidder { get; private set; }
    [Networked] public PlayerRef Winner { get; private set; }
    [Networked] public AuctionState State { get; private set; }

    [Networked]
    private TickTimer AuctionTimer { get; set; }

    // =========================================================
    // 1. CONFIGURE PARTICIPANTS
    // =========================================================

    public bool ConfigurePlayers(params PlayerRef[] players)
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only State Authority can configure players."
            );
            return false;
        }

        if (State == AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] Cannot change players during an active auction."
            );
            return false;
        }

        if (players == null ||
            players.Length < 2 ||
            players.Length > MaxPlayers)
        {
            Debug.LogWarning(
                "[NetworkTrading] Auction requires 2 to 4 players."
            );
            return false;
        }

        // Xóa danh sách người chơi cũ.
        for (int i = 0; i < MaxPlayers; i++)
        {
            Participants.Set(i, PlayerRef.None);
        }

        ParticipantCount = 0;

        // Kiểm tra từng người chơi.
        for (int i = 0; i < players.Length; i++)
        {
            PlayerRef player = players[i];

            if (player == PlayerRef.None)
            {
                Debug.LogWarning(
                    "[NetworkTrading] Invalid PlayerRef."
                );

                ClearParticipants();
                return false;
            }

            if (players.Take(i).Contains(player))
            {
                Debug.LogWarning(
                    $"[NetworkTrading] Duplicate player: {player}"
                );

                ClearParticipants();
                return false;
            }

            if (!Runner.ActivePlayers.Contains(player))
            {
                Debug.LogWarning(
                    $"[NetworkTrading] Player is not connected: {player}"
                );

                ClearParticipants();
                return false;
            }

            Participants.Set(ParticipantCount, player);
            ParticipantCount++;
        }

        Debug.Log(
            $"[NetworkTrading] Participants Configured | " +
            $"Count={ParticipantCount}"
        );

        return true;
    }

    private void ClearParticipants()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            Participants.Set(i, PlayerRef.None);
        }

        ParticipantCount = 0;
    }

    private bool IsParticipant(PlayerRef player)
    {
        for (int i = 0; i < ParticipantCount; i++)
        {
            if (Participants[i] == player)
                return true;
        }

        return false;
    }

    // =========================================================
    // 2. START AUCTION
    // =========================================================

    public bool TradeItem(int itemId, int quantity, int price)
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only State Authority can start an auction."
            );
            return false;
        }

        if (State == AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] An auction is already active."
            );
            return false;
        }

        if (ParticipantCount < 2 || ParticipantCount > MaxPlayers)
        {
            Debug.LogWarning(
                "[NetworkTrading] Auction requires 2 to 4 participants."
            );
            return false;
        }

        // Kiểm tra người chơi vẫn đang kết nối.
        for (int i = 0; i < ParticipantCount; i++)
        {
            if (!Runner.ActivePlayers.Contains(Participants[i]))
            {
                Debug.LogWarning(
                    "[NetworkTrading] A participant has disconnected."
                );
                return false;
            }
        }

        if (itemSO == null)
        {
            Debug.LogWarning("[NetworkTrading] ItemSO is missing.");
            return false;
        }

        if (itemSO.itemType != ItemType.Valuable)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only Valuable items can be auctioned."
            );
            return false;
        }

        if (itemId <= 0 || quantity <= 0 || price <= 0)
        {
            Debug.LogWarning(
                "[NetworkTrading] Invalid item ID, quantity, or price."
            );
            return false;
        }

        ItemID = itemId;
        Quantity = quantity;
        StartingPrice = price;
        CurrentBid = price;

        HighestBidder = PlayerRef.None;
        Winner = PlayerRef.None;

        AuctionTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(0.1f, auctionDuration)
        );

        State = AuctionState.Active;

        Debug.Log(
            $"[NetworkTrading] Auction Started | " +
            $"ItemID={ItemID} | Quantity={Quantity} | " +
            $"StartingPrice={StartingPrice} | " +
            $"Participants={ParticipantCount}"
        );

        return true;
    }

    // =========================================================
    // 3. REQUEST BID
    // =========================================================

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestBid(int bidAmount, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerRef bidder = info.Source;

        if (State != AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] Cannot bid. Auction is not active."
            );
            return;
        }

        if (!IsParticipant(bidder))
        {
            Debug.LogWarning(
                $"[NetworkTrading] Unauthorized bidder: {bidder}"
            );
            return;
        }

        if (!Runner.ActivePlayers.Contains(bidder))
        {
            Debug.LogWarning(
                $"[NetworkTrading] Bidder disconnected: {bidder}"
            );
            return;
        }

        if (AuctionTimer.ExpiredOrNotRunning(Runner))
        {
            FinishAuction();
            return;
        }

        if (bidAmount <= CurrentBid)
        {
            Debug.LogWarning(
                $"[NetworkTrading] Invalid bid | " +
                $"Bidder={bidder} | Bid={bidAmount} | " +
                $"CurrentBid={CurrentBid}"
            );
            return;
        }

        CurrentBid = bidAmount;
        HighestBidder = bidder;

        AuctionTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(0.1f, auctionDuration)
        );

        Debug.Log(
            $"[NetworkTrading] New Bid | " +
            $"Bidder={bidder} | CurrentBid={CurrentBid} | " +
            $"TimeReset={auctionDuration}s"
        );
    }

    // =========================================================
    // 4. MONITOR AUCTION AND DISCONNECTIONS
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != AuctionState.Active)
            return;

        RemoveDisconnectedParticipants();

        // Việc xử lý thoát phòng có thể đã kết thúc phiên.
        if (State != AuctionState.Active)
            return;

        if (AuctionTimer.ExpiredOrNotRunning(Runner))
        {
            FinishAuction();
        }
    }

    private void RemoveDisconnectedParticipants()
    {
        // Duyệt ngược để xóa phần tử an toàn khi dồn mảng.
        for (int i = ParticipantCount - 1; i >= 0; i--)
        {
            PlayerRef player = Participants[i];

            if (!Runner.ActivePlayers.Contains(player))
            {
                Debug.Log(
                    $"[NetworkTrading] Participant Disconnected | " +
                    $"Player={player}"
                );

                RemoveParticipantAt(i);
            }
        }

        // Nếu còn dưới 2 người thì kết thúc phiên.
        if (ParticipantCount < 2)
        {
            if (ParticipantCount == 1)
            {
                Winner = Participants[0];

                Debug.Log(
                    $"[NetworkTrading] Auction Completed | " +
                    $"Reason=Not Enough Participants | " +
                    $"Winner={Winner}"
                );
            }
            else
            {
                Winner = PlayerRef.None;

                Debug.Log(
                    "[NetworkTrading] Auction Ended | " +
                    "No participants remaining."
                );
            }

            State = AuctionState.Completed;
        }
    }

    private void RemoveParticipantAt(int index)
    {
        PlayerRef removedPlayer = Participants[index];

        // Dồn các phần tử phía sau lên trước.
        for (int i = index; i < ParticipantCount - 1; i++)
        {
            Participants.Set(i, Participants[i + 1]);
        }

        ParticipantCount--;

        Participants.Set(ParticipantCount, PlayerRef.None);

        // Nếu người giữ giá cao nhất thoát, xóa lượt giá đó.
        // Phiên tiếp tục với giá khởi điểm để những người còn lại
        // có thể đặt giá mới.
        if (HighestBidder == removedPlayer)
        {
            HighestBidder = PlayerRef.None;
            CurrentBid = StartingPrice;

            Debug.Log(
                "[NetworkTrading] Highest bidder disconnected | " +
                "Current bid reset to starting price."
            );
        }
    }

    // =========================================================
    // 5. FINISH AUCTION WHEN TIMER EXPIRES
    // =========================================================

    private void FinishAuction()
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != AuctionState.Active)
            return;

        if (HighestBidder == PlayerRef.None ||
            !IsParticipant(HighestBidder) ||
            !Runner.ActivePlayers.Contains(HighestBidder))
        {
            Winner = PlayerRef.None;

            Debug.Log(
                "[NetworkTrading] Auction Ended | " +
                "No valid bids | No winner."
            );
        }
        else
        {
            Winner = HighestBidder;

            Debug.Log(
                $"[NetworkTrading] Auction Completed | " +
                $"Winner={Winner} | WinningBid={CurrentBid} | " +
                $"ItemID={ItemID} | Quantity={Quantity}"
            );
        }

        State = AuctionState.Completed;
    }

    // =========================================================
    // 6. GET REMAINING TIME
    // =========================================================

    public float GetRemainingTime()
    {
        if (Object == null || !Object.IsValid)
            return 0f;

        if (State != AuctionState.Active)
            return 0f;

        return AuctionTimer.RemainingTime(Runner) ?? 0f;
    }
}
