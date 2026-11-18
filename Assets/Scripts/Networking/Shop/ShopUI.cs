using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] ItemShopDatabase shopDatabase;
    [SerializeField] UIItemShop[] itemElements;   // kéo các UI Item đã tạo sẵn vào đây (theo thứ tự layout)
    [SerializeField] ShopManager manager;
    [SerializeField] UIBanedElement[] bannedElements;
    [SerializeField] Sprite defaultAvt;
    // Gọi khi mở shop (từ SnowmanNPC.ShowUI(true) chẳng hạn)
    //public void Show()
    //{
    //    Refresh();
    //    gameObject.SetActive(true);
    //}

    //public void Hide()
    //{
    //    gameObject.SetActive(false);
    //}
    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshItem();
        RefreshBannedList();        
    }

    void RefreshItem()
    {
        if (shopDatabase == null || itemElements == null) return;
        int count = shopDatabase.itemShops.Count;

        for (int i = 0; i < itemElements.Length; i++)
        {
            if (i < count)
            {
                itemElements[i].Setup(shopDatabase.itemShops[i], manager);
            }
            else
            {
                itemElements[i].Clear();
            }
        }
    }

    void RefreshBannedList()
    {
        if (manager == null || bannedElements == null) return;
        var bannedLists = manager.BannedLists;
        int index = 0;
        for (int i = 0; i< bannedLists.Count; i++)
        {
            PlayerRef playerRef = bannedLists[i];
            if (playerRef == PlayerRef.None) continue;

            if (index >= bannedElements.Length) break;

            string name = manager.Runner.GetPlayerObject(playerRef).GetBehaviour<NetworkPlayer>().Name.Value;

            bannedElements[index].Setup(string.IsNullOrEmpty(name) ? playerRef.ToString() : name, defaultAvt);
            index++;
        }
        // Ẩn các element còn dư
        for (int i = index; i < bannedElements.Length; i++)
        {
            bannedElements[i].Clear();
        }
    }


    // Nếu muốn cập nhật lại khi database thay đổi runtime
    public void SetDatabase(ItemShopDatabase newDatabase)
    {
        shopDatabase = newDatabase;
        Refresh();
    }  
}