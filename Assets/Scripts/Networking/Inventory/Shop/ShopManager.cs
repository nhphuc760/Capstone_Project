using Fusion;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : NetworkBehaviour
{
    [Header("Catalog Reference")]
    [SerializeField] private ShopCatalogSO catalog;

    [Header("Network Session References")]
    [SerializeField] private TradeSession tradeSystem;
    [SerializeField] private ShopSession shopSession;
    [SerializeField] private NetworkMoney playerMoney;
    [SerializeField] private NetworkInventory playerInventory;

    [Header("Trade Session UI References")]
    [SerializeField] private GameObject tradeSessionUI;
    [SerializeField] private Button StartSessionBTN;
    [SerializeField] private Button CancelBTN;
    [SerializeField] private TMP_Text amountTxt;
    [SerializeField] private Button BiddingBTN;
    [SerializeField] private Button MinusBTN;
    [SerializeField] private Button PlusBTN;
    [SerializeField] private Button CompleteBTN;

    [Header("Trade Session Settings (Valuable Item)")]
    [Tooltip("ID của món đồ Valuable đem ra đấu giá")]
    [SerializeField] private int tradeItemID = 1;
    [Min(1), SerializeField] private int tradeItemAmount = 1;
    [SerializeField] private int bidStep = 10;

    [Header("Buy Sell Session UI References")]
    [SerializeField] private GameObject buySellSessionUI;
    [SerializeField] private Button BuyBTN;
    [SerializeField] private Button SellBTN;

    [Header("Buy Sell Session Settings (Consumable Item)")]
    [Tooltip("ID của món đồ Consumable trong Shop")]
    [SerializeField] private int shopItemID = 1;
    [Min(1), SerializeField] private int shopItemAmount = 1;

    private int currentBidAmount;

    private void Start()
    {
        #region Trade Session UI Listeners
        if (StartSessionBTN != null) StartSessionBTN.onClick.AddListener(StartTradeSession);
        if (BiddingBTN != null)      BiddingBTN.onClick.AddListener(PlaceBid);
        if (CancelBTN != null)       CancelBTN.onClick.AddListener(CancelSession);
        if (MinusBTN != null)        MinusBTN.onClick.AddListener(DecreaseBid);
        if (PlusBTN != null)         PlusBTN.onClick.AddListener(IncreaseBid);
        #endregion

        #region Buy/Sell Session UI Listeners
        if (BuyBTN != null)          BuyBTN.onClick.AddListener(Buyer);
        if (SellBTN != null)         SellBTN.onClick.AddListener(Seller);
        #endregion

        tradeSessionUI.SetActive(false);
        buySellSessionUI.SetActive(false);
        UpdateBidUI();
    }

    private void Update()
    {
        OpenTradeSettings();
        OpenBuySellSettings();
    }

    #region Bid Management (Valuable Items)
    private void StartTradeSession()
    {
        if (!CanStartSession())
            return;

        // Kiểm tra xem item đưa vào trade có phải là đồ Valuable hợp lệ trong Catalog không
        if (catalog != null && catalog.GetValuableOffer(tradeItemID) == null)
        {
            Debug.LogError($"[ShopManager] Không thể bắt đầu Trade: ItemID {tradeItemID} không phải là Valuable trong Catalog!");
            return;
        }

        tradeSystem.StartSession();
        InitializeBid();
    }

    private void InitializeBid()
    {
        if (!CanInitializeBid())
            return;

        // Nếu sàn đấu giá đã có giá hiện tại, mức bid mới = CurrentBid + step
        int baseBid = tradeSystem.CurrentBid;

        // Nếu phiên mới bắt đầu chưa có bid, lấy giá sàn từ Catalog
        if (baseBid <= 0 && catalog != null)
        {
            ShopItemOffer valuableOffer = catalog.GetValuableOffer(tradeItemID);
            if (valuableOffer != null)
                baseBid = valuableOffer.SellPrice * tradeItemAmount;
        }

        currentBidAmount = baseBid + bidStep;
        UpdateBidUI();
    }

    private void IncreaseBid()
    {
        if (!CanChangeBid())
            return;

        currentBidAmount += bidStep;
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

        tradeSystem.PlaceBid(currentBidAmount);
        currentBidAmount += bidStep;
        UpdateBidUI();
    }

    private void CancelSession()
    {
        if (!CanCancelSession())
            return;

        tradeSystem.QuitSession();
    }


    private void UpdateBidUI()
    {
        if (amountTxt != null)
            amountTxt.text = currentBidAmount.ToString();
    }
    #endregion

    #region Buy / Sell Management (Consumable Items)
    private void Buyer()
    {
        if (shopSession == null) return;

        // Xác thực món hàng phải là Consumable trong Catalog
        if (catalog != null && catalog.GetConsumableOffer(shopItemID) == null)
        {
            Debug.LogError($"[ShopManager] Không thể mua: ItemID {shopItemID} không phải là Consumable hợp lệ!");
            return;
        }

        shopSession.RPC_Buy(shopItemID, shopItemAmount);
    }

    private void Seller()
    {
        if (shopSession == null) return;

        // Xác thực món hàng phải là Consumable trong Catalog
        if (catalog != null && catalog.GetConsumableOffer(shopItemID) == null)
        {
            Debug.LogError($"[ShopManager] Không thể bán: ItemID {shopItemID} không phải là Consumable hợp lệ!");
            return;
        }

        shopSession.RPC_Sell(shopItemID, shopItemAmount);
    }
    #endregion

    #region Validation Checks
    public bool CanStartSession() => tradeSystem != null && !tradeSystem.IsActive;
    public bool CanInitializeBid() => tradeSystem != null && tradeSystem.IsActive;
    public bool CanChangeBid() => tradeSystem != null && tradeSystem.IsActive;
    public bool CanCancelSession() => tradeSystem != null && tradeSystem.IsActive;
    public bool CanCompleteSession() => tradeSystem != null && tradeSystem.IsActive;

    public bool CanPlaceBid()
    {
        if (tradeSystem == null || !tradeSystem.IsActive)
            return false;

        return MoneyCheck(currentBidAmount);
    }

    private bool MoneyCheck(int bidAmount)
    {
        if (playerMoney == null)
        {
            Debug.LogWarning("[ShopManager] MoneyCheck failed | NetworkMoney reference is null");
        return false;
        }

    if (!playerMoney.HasMoney(bidAmount))
    {
        Debug.Log("[ShopManager] MoneyCheck failed | Not enough money");
        return false;
    }

    return true;

    }
    #endregion

    #region Settings
    private void OpenTradeSettings()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            tradeSessionUI.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            tradeSessionUI.SetActive(false);
        }
    }

    private void OpenBuySellSettings()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            buySellSessionUI.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            buySellSessionUI.SetActive(false);
        }
    }
    #endregion
}