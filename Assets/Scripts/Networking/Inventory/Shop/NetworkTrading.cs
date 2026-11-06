
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

    #region AUCTION SETTINGS
    [Header("Auction Settings")]
    [SerializeField] private ValuableShopSO valuableShopSO;
    [SerializeField] private float auctionDuration = 10f;
    #endregion

    #region PARTICIPANTS
    [Header("Participants")]

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<PlayerRef> Participants => default;

    [Networked]
    public int ParticipantCount { get; private set; }
    #endregion

    #region AUCTION DATA
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

    public bool IsAuctionActive => State == AuctionState.Active;
    #endregion

    #region CONFIGURE PARTICIPANTS
    public bool ConfigurePlayers(params PlayerRef[] players)
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only State Authority can configure players.");

            return false;
        }

        if (State == AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] Cannot change players during an active auction.");

            return false;
        }

        if (players == null || players.Length < 2 || players.Length > MaxPlayers)
        {
            Debug.LogWarning("[NetworkTrading] Auction requires 2 to 4 players.");

            return false;
        }

        ClearParticipants();

        for (int i = 0; i < players.Length; i++)
        {
            PlayerRef player = players[i];

            if (player == PlayerRef.None)
            {
                Debug.LogWarning(
                    "[NetworkTrading] Invalid PlayerRef.");

                ClearParticipants();
                return false;
            }

            if (players.Take(i).Contains(player))
            {
                Debug.LogWarning(
                    $"[NetworkTrading] Duplicate player: {player}");

                ClearParticipants();
                return false;
            }

            if (!Runner.ActivePlayers.Contains(player))
            {
                Debug.LogWarning(
                    $"[NetworkTrading] Player is not connected: {player}");

                ClearParticipants();
                return false;
            }

            Participants.Set(ParticipantCount, player);
            ParticipantCount++;
        }

        Debug.Log(
            $"[NetworkTrading] Participants Configured | " +
            $"Count={ParticipantCount}");

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
    #endregion

    #region START AUCTION
    public bool TradeItem(int itemId, int quantity)
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only State Authority can start an auction.");

            return false;
        }

        if (State == AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] An auction is already active.");

            return false;
        }

        if (ParticipantCount < 2 || ParticipantCount > MaxPlayers)
        {
            Debug.LogWarning(
                "[NetworkTrading] Auction requires 2 to 4 participants.");

            return false;
        }

        for (int i = 0; i < ParticipantCount; i++)
        {
            if (!Runner.ActivePlayers.Contains(Participants[i]))
            {
                Debug.LogWarning(
                    "[NetworkTrading] A participant has disconnected.");

                return false;
            }
        }

        if (valuableShopSO == null)
        {
            Debug.LogError(
                "[NetworkTrading] ValuableShopSO is missing.");

            return false;
        }

        if (itemId <= 0 || quantity <= 0)
        {
            Debug.LogWarning(
                "[NetworkTrading] Invalid item ID or quantity.");

            return false;
        }

        if (!valuableShopSO.TryGetItem(
            itemId,
            out ValuableShopSO.ValuableShopItem shopItem))
        {
            Debug.LogWarning(
                $"[NetworkTrading] ItemID={itemId} not found in ValuableShopSO.");

            return false;
        }

        if (shopItem.itemSO == null ||
            shopItem.itemSO.itemType != ItemType.Valuable)
        {
            Debug.LogWarning(
                "[NetworkTrading] Only Valuable items can be auctioned.");

            return false;
        }

        int price = shopItem.valuablePrice;

        if (price <= 0)
        {
            Debug.LogWarning(
                $"[NetworkTrading] Invalid valuable price: {price}");

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
            Mathf.Max(0.1f, auctionDuration));

        State = AuctionState.Active;

        Debug.Log(
            $"[NetworkTrading] Auction Started | " +
            $"Item={shopItem.itemSO.name} | " +
            $"ItemID={ItemID} | Quantity={Quantity} | " +
            $"StartingPrice={StartingPrice} | " +
            $"Participants={ParticipantCount}");

        return true;
    }
    #endregion

    #region BID REQUEST
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestBid(
        int bidAmount,
        RpcInfo info = default)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        PlayerRef bidder = info.Source;

        if (bidder == PlayerRef.None)
        {
            Debug.LogWarning(
                "[NetworkTrading] Invalid bidder: PlayerRef is None.");

            return;
        }

        if (State != AuctionState.Active)
        {
            Debug.LogWarning(
                "[NetworkTrading] Cannot bid. Auction is not active.");

            return;
        }

        if (!IsParticipant(bidder))
        {
            Debug.LogWarning(
                $"[NetworkTrading] Unauthorized bidder: {bidder}");

            return;
        }

        if (!Runner.ActivePlayers.Contains(bidder))
        {
            Debug.LogWarning(
                $"[NetworkTrading] Bidder disconnected: {bidder}");

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
                $"CurrentBid={CurrentBid}");

            return;
        }

        // Kiểm tra người chơi có đủ Gold để thanh toán mức giá mới.
        if (!CanPlayerAffordBid(bidder, bidAmount))
        {
            return;
        }

        // Chỉ cập nhật giá, chưa trừ Gold.
        CurrentBid = bidAmount;
        HighestBidder = bidder;

        AuctionTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(0.1f, auctionDuration));

        Debug.Log(
            $"[NetworkTrading] New Bid | " +
            $"Bidder={bidder} | CurrentBid={CurrentBid} | " +
            $"TimeReset={auctionDuration}s");
    }
    #endregion

    #region WALLET VALIDATION
    private bool CanPlayerAffordBid(
        PlayerRef player,
        int bidAmount)
    {
        if (player == PlayerRef.None || bidAmount <= 0)
            return false;

        if (!Runner.TryGetPlayerObject(
            player,
            out NetworkObject playerObject))
        {
            Debug.LogError(
                $"[NetworkTrading] PlayerObject not found: {player}");

            return false;
        }

        NetworkResourceWallet wallet =
            playerObject.GetComponent<NetworkResourceWallet>();

        if (wallet == null)
        {
            Debug.LogError(
                $"[NetworkTrading] Wallet not found: {player}");

            return false;
        }

        if (!wallet.CanAffordGold(bidAmount))
        {
            Debug.Log(
                $"[NetworkTrading] BID REJECTED | " +
                $"Player={player} | Required={bidAmount} | " +
                $"Available={wallet.GetTotalGoldValue()}");

            return false;
        }

        Debug.Log(
            $"[NetworkTrading] BID AFFORDABLE | " +
            $"Player={player} | Bid={bidAmount}");

        return true;
    }
    #endregion

    #region AUCTION UPDATE
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != AuctionState.Active)
            return;

        RemoveDisconnectedParticipants();

        if (State != AuctionState.Active)
            return;

        if (AuctionTimer.ExpiredOrNotRunning(Runner))
        {
            FinishAuction();
        }
    }
    #endregion

    #region DISCONNECTED PARTICIPANTS
    private void RemoveDisconnectedParticipants()
    {
        for (int i = ParticipantCount - 1; i >= 0; i--)
        {
            PlayerRef player = Participants[i];

            if (!Runner.ActivePlayers.Contains(player))
            {
                Debug.Log(
                    $"[NetworkTrading] Participant Disconnected | " +
                    $"Player={player}");

                RemoveParticipantAt(i);
            }
        }

        if (ParticipantCount < 2)
        {
            // Không tự động trao chiến thắng cho người còn lại
            // nếu chưa có lượt đặt giá hợp lệ.
            if (HighestBidder != PlayerRef.None &&
                IsParticipant(HighestBidder))
            {
                Winner = HighestBidder;

                Debug.Log(
                    $"[NetworkTrading] Auction Completed | " +
                    $"Reason=Not Enough Participants | " +
                    $"Winner={Winner} | WinningBid={CurrentBid}");
            }
            else
            {
                Winner = PlayerRef.None;

                Debug.Log(
                    "[NetworkTrading] Auction Ended | " +
                    "Not enough participants and no valid bid.");
            }

            State = AuctionState.Completed;
        }
    }

    private void RemoveParticipantAt(int index)
    {
        PlayerRef removedPlayer = Participants[index];

        for (int i = index; i < ParticipantCount - 1; i++)
        {
            Participants.Set(i, Participants[i + 1]);
        }

        ParticipantCount--;

        Participants.Set(ParticipantCount, PlayerRef.None);

        if (HighestBidder == removedPlayer)
        {
            HighestBidder = PlayerRef.None;
            CurrentBid = StartingPrice;

            Debug.Log(
                "[NetworkTrading] Highest bidder disconnected | " +
                "Current bid reset to starting price.");
        }
    }
    #endregion

    #region FINISH AUCTION
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
                "No valid bids | No winner.");
        }
        else
        {
            Winner = HighestBidder;

            Debug.Log(
                $"[NetworkTrading] Auction Completed | " +
                $"Winner={Winner} | WinningBid={CurrentBid} | " +
                $"ItemID={ItemID} | Quantity={Quantity}");
        }

        State = AuctionState.Completed;
    }
    #endregion

    #region TIMER
    public float GetRemainingTime()
    {
        if (Object == null || !Object.IsValid)
            return 0f;

        if (State != AuctionState.Active)
            return 0f;

        return AuctionTimer.RemainingTime(Runner) ?? 0f;
    }
    #endregion
}
