using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Navigation")]
    [SerializeField] private Button tradingButton;
    [SerializeField] private Button shoppingButton;
    [SerializeField] private Button closeButton;

    [Header("Shop Panels")]
    [SerializeField] private GameObject tradingPanel;
    [SerializeField] private GameObject shoppingPanel;
    
    private void Awake()
    {
        ShowTradingPanel(false);
        ShowShoppingPanel(false);
    }

    private void Update()
    {
        if (tradingButton != null)
            tradingButton.onClick.AddListener(OpenTrading);

        if (shoppingButton != null)
            shoppingButton.onClick.AddListener(OpenShopping);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
    }

    private void OpenTrading()
    {
        ShowTradingPanel(true);
        ShowShoppingPanel(false);

        Debug.Log("[ShopManager] Trading panel opened.");
    }

    private void OpenShopping()
    {
        ShowTradingPanel(false);
        ShowShoppingPanel(true);

        Debug.Log("[ShopManager] Shopping panel opened.");
    }

    private void CloseShop()
    {
        ShowTradingPanel(false);
        ShowShoppingPanel(false);

        Debug.Log("[ShopManager] All shop panels closed.");
    }

    private void ShowTradingPanel(bool isOpen)
    {
        if (tradingPanel != null)
            tradingPanel.SetActive(isOpen);
    }

    private void ShowShoppingPanel(bool isOpen)
    {
        if (shoppingPanel != null)
            shoppingPanel.SetActive(isOpen);
    }
}
