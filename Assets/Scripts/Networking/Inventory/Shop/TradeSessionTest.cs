using UnityEngine;

public class TradeSessionTest : MonoBehaviour
{
    [SerializeField] private TradeSystem tradeSystem;

    private void Update()
    {
        #region Input Debugging
        // Start Auction
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Press S");
            StartAuction();
        }

        // Bid
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Press 1");
            PlaceBid(20);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Press 2");
            PlaceBid(30);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Press 3");
            PlaceBid(50);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Press 4"); 
            PlaceBid(75);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Press 5");
            PlaceBid(100);
        }

        // Complete
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Press C");
            CompleteAuction();
        }

        // Cancel
        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Press X");
            CancelAuction();
        }

        // Reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Press R", this);
            ResetAuction();
        }

        // Show status
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Press P", this);
            ShowStatus();
        }
        #endregion
    }

    private void StartAuction()
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        Debug.Log("[AuctionDebug] Request Start Auction");

        tradeSystem.RPC_RequestStartTestSession();
    }

    private void PlaceBid(int amount)
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        if (!tradeSystem.IsActive)
        {
            Debug.LogWarning("[AuctionDebug] Auction is not active.");
            return;
        }

        Debug.Log($"[AuctionDebug] Place Bid | Amount={amount}");

        tradeSystem.RPC_PlaceBid(amount);
    }

    private void CompleteAuction()
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        Debug.Log("[AuctionDebug] Request Complete Auction");

        tradeSystem.RPC_CompleteSession();
    }

    private void CancelAuction()
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        Debug.Log("[AuctionDebug] Request Cancel Auction");

        tradeSystem.RPC_CancelSession();
    }

    private void ResetAuction()
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        tradeSystem.ResetSession();

        Debug.Log("[AuctionDebug] Auction Reset");
    }

    private void ShowStatus()
    {
        if (tradeSystem == null)
        {
            Debug.LogError("[AuctionDebug] TradeSystem is NULL.");
            return;
        }

        Debug.Log(
            $"[AuctionDebug] " +
            $"State={tradeSystem.State} | " +
            $"Player1={tradeSystem.Player1} | " +
            $"Player2={tradeSystem.Player2} | " +
            $"ItemID={tradeSystem.ItemID} | " +
            $"Amount={tradeSystem.ItemAmount} | " +
            $"CurrentBid={tradeSystem.CurrentBid} | " +
            // $"CurrentBidder={tradeSystem.CurrentBidder} | " +
            $"Winner={tradeSystem.GetWinningPlayer()}"
        );
    }
}