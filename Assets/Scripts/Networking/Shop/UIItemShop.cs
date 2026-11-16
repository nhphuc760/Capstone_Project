using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemShop : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI priceText;   // hoặc UI riêng cho NetworkResourcePlayer nếu cần
    [SerializeField] Button onBuyClick;
    ItemShop currentData;

    public void Setup(ItemShop data, ShopManager manager)
    {
        currentData = data;

        if (data == null || data.itemSO == null)
        {
            gameObject.SetActive(false);
            return;
        }

        //Button
        onBuyClick.onClick.AddListener(() => OnBuyClicked(manager));

        // Icon
        if (icon != null)
            icon.sprite = data.itemSO.Icon;          // giả sử ItemSO có field icon

        // Name
        if (nameText != null)
            nameText.text = data.itemSO.ItemName;    // hoặc data.itemSO.name

        // Description
        if (descriptionText != null)
            descriptionText.text = data.itemSO.Description;

        // Price – tùy cách bạn hiển thị NetworkResourcePlayer
        IconTMP iconTMP = manager.iconTMP;
        string s = "";
        s += data.price.Wood > 0 ? $"{iconTMP.GetResourceCode(ResourceType.Wood)} {data.price.Wood} " : "";
        s += data.price.Stone > 0 ? $"{iconTMP.GetResourceCode(ResourceType.Stone)} {data.price.Stone} " : "";
        s += data.price.GoldOre > 0 ? $"{iconTMP.GetResourceCode(ResourceType.Gold)} {data.price.GoldOre} " : "";
        s += data.price.Electric > 0 ? $"{iconTMP.GetResourceCode(ResourceType.Electric)} {data.price.Electric}" : "";
        if (priceText != null)
        {
            priceText.text = s;
        }           
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        currentData = null;
        onBuyClick.onClick.RemoveAllListeners();
        gameObject.SetActive(false);

    }

    // Gọi khi bấm mua (nếu bạn muốn gắn button ở đây)
    public void OnBuyClicked(ShopManager manager)
    {
        if (currentData == null) return;
        manager.BuyItem(currentData);

        // Gọi RPC / event mua hàng ở đây
        // ShopManager.Instance.RequestBuy(currentData);
    }
}