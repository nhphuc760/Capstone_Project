using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TradeSession tradeSystem;
    [SerializeField] private ShopSession shopSession;
    [SerializeField] private NetworkMoney playerMoney;
    [SerializeField] private NetworkInventory playerInventory;

    [Header("Trade Session References")]
    [SerializeField] private Button StartSessionBTN;
    [SerializeField] private Button CancelBTN;
    [SerializeField] private TMP_Text amountTxt;
    [SerializeField] private Button BiddingBTN;
    [SerializeField] private Button MinusBTN;
    [SerializeField] private Button PlusBTN;

    [Header("Trade Session Settings")]
    [SerializeField] private int itemID = 1;
    [SerializeField] private int itemAmount = 1;
    [SerializeField] private int bidStep = 10;

    [Header("Buy Sell Session References")]
    [SerializeField] private Button BuyBTN;
    [SerializeField] private Button SellBTN;

    [Header("Buy Sell Session Settings")]
    [SerializeField] private int shopItemID;
    [Min(1), SerializeField] private int shopItemAmount = 1;

    private int currentBidAmount;

    private void Start()
    {
        StartSessionBTN.onClick.AddListener(StartTradeSession);
        BiddingBTN.onClick.AddListener(PlaceBid);
        CancelBTN.onClick.AddListener(CancelSession);
        MinusBTN.onClick.AddListener(DecreaseBid);
        PlusBTN.onClick.AddListener(IncreaseBid);
        BuyBTN.onClick.AddListener(Buyer);
        SellBTN.onClick.AddListener(Seller);

        UpdateBidUI();
    }

    #region Bid Management
    #region Start Trade Session
    private void StartTradeSession()
    {
        if (!CanStartSession())
            return;

        tradeSystem.RPC_RequestStartTestSession();

        InitializeBid();
    }
    #endregion

    #region Bid Setup
    private void InitializeBid()
    {
        if (!CanInitializeBid())
            return;

        currentBidAmount = tradeSystem.CurrentBid + bidStep;

        UpdateBidUI();
    }
    #endregion

    #region Bid Amount
    private void IncreaseBid()
    {
        if (!CanChangeBid())
            return;

        currentBidAmount += bidStep;

        int minimumBid = tradeSystem.CurrentBid + bidStep;

        if (currentBidAmount < minimumBid)
            currentBidAmount = minimumBid;

        UpdateBidUI();
    }

    private void DecreaseBid()
    {
        if (!CanChangeBid())
            return;

        int minimumBid = tradeSystem.CurrentBid + bidStep;

        currentBidAmount -= bidStep;

        if (currentBidAmount < minimumBid)
            currentBidAmount = minimumBid;

        UpdateBidUI();
    }

    private void UpdateBidUI()
    {
        if (amountTxt == null)
            return;

        amountTxt.text = currentBidAmount.ToString();
    }
    #endregion

    #region Place Bid
    private void PlaceBid()
    {
        if (!CanPlaceBid())
            return;

        int minimumBid = tradeSystem.CurrentBid + bidStep;

        if (currentBidAmount < minimumBid)
        {
            currentBidAmount = minimumBid;
            UpdateBidUI();
            return;
        }

        tradeSystem.RPC_PlaceBid(currentBidAmount);

        currentBidAmount += bidStep;

        UpdateBidUI();
    }
    #endregion

    #region Cancel Session
    private void CancelSession()
    {
        if (!CanCancelSession())
            return;

        tradeSystem.RPC_CancelSession();
    }
    #endregion

    #region Complete Session
    private void WinningBidder()
    {
        if (!CanCompleteSession())
            return;

        tradeSystem.RPC_CompleteSession();
    }
    #endregion
    #endregion

    #region Buy Sell Management
    #region Buyer
    private void Buyer()
    {
        if (shopSession == null)
            return;

        shopSession.RPC_Buy(shopItemID, shopItemAmount);
    }
    #endregion

    #region Seller
    private void Seller()
    {
        if (shopSession == null)
            return;

        shopSession.RPC_Sell(shopItemID, shopItemAmount);
    }
    #endregion
    #endregion

    #region Validation
    public bool CanStartSession()
    {
        if (tradeSystem == null)
            return false;

        if (tradeSystem.IsActive)
            return false;

        return true;
    }

    public bool CanInitializeBid()
    {
        if (tradeSystem == null)
            return false;

        if (!tradeSystem.IsActive)
            return false;

        return true;
    }

    public bool CanChangeBid()
    {
        if (tradeSystem == null)
            return false;

        if (!tradeSystem.IsActive)
            return false;

        return true;
    }

    public bool CanPlaceBid()
    {
        if (tradeSystem == null)
            return false;

        if (!tradeSystem.IsActive)
            return false;

        if (!MoneyCheck(currentBidAmount))
            return false;

        return true;
    }

    public bool CanCancelSession()
    {
        if (tradeSystem == null)
            return false;

        if (!tradeSystem.IsActive)
            return false;

        return true;
    }

    public bool CanCompleteSession()
    {
        if (tradeSystem == null)
            return false;

        if (!tradeSystem.IsActive)
            return false;

        return true;
    }

    private bool MoneyCheck(int bidAmount)
    {
        if (playerMoney == null)
            return false;

        return playerMoney.HasMoney(bidAmount);
    }
    #endregion
}
