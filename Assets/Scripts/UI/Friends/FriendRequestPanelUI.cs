using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] FriendRequestUI frequestUIPrefabs;
    [SerializeField] Image announceIcon;
    [SerializeField] GameObject emptyStateText;

    private readonly Dictionary<string, FriendRequestUI> requestItems = new Dictionary<string, FriendRequestUI>();

    private void Awake()
    {
        var makeFriend = NetworkDataManager.Instance.friendManager.MakeFriend;
        makeFriend.onChildAdded += InstantiateElement;
        makeFriend.onChildRemoved += RemoveElement;

        var list = makeFriend.GetFriendRequests();
        if (list != null)
        {
            foreach (var id in list)
                InstantiateElement(id);
        }

        UpdateAnnouce();
    }

    public void UpdateAnnouce()
    {
        bool hasRequests = requestItems.Count > 0;
        if (announceIcon != null)
            announceIcon.gameObject.SetActive(hasRequests);
        if (emptyStateText != null)
            emptyStateText.SetActive(!hasRequests);
    }

    private void InstantiateElement(string userID)
    {
        if (string.IsNullOrEmpty(userID) || requestItems.ContainsKey(userID)) return;

        var obj = Instantiate(frequestUIPrefabs, content);
        obj.ResetUI();
        obj.UpdateUI(userID);
        requestItems[userID] = obj;
        UpdateAnnouce();
    }

    private void RemoveElement(string userID)
    {
        if (requestItems.TryGetValue(userID, out var item))
        {
            requestItems.Remove(userID);
            if (item != null)
                Destroy(item.gameObject);
        }
        UpdateAnnouce();
    }

    private void OnDestroy()
    {
        if (NetworkDataManager.Instance?.friendManager?.MakeFriend != null)
        {
            var makeFriend = NetworkDataManager.Instance.friendManager.MakeFriend;
            makeFriend.onChildAdded -= InstantiateElement;
            makeFriend.onChildRemoved -= RemoveElement;
        }
    }
}