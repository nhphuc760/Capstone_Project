using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickFriendUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] RectTransform Content;
    [SerializeField] Sprite defaultAvatar;
    [SerializeField] GameObject expandFriendPanel;
    Dictionary<string, Image> container = new();
    EventBinding<DataEvent.OnFriendAdded> onFriendAdded;
    EventBinding<DataEvent.OnFriendRemoved> onFriendRemoved;


    public void OnPointerClick(PointerEventData eventData)
    {
        expandFriendPanel.SetActive(true);
        Debug.Log("PointerClick");
    }

    private void Awake()
    {
        foreach (var i in NetworkDataManager.Instance.friendManager.GetFriends())
        {
            var avt = NetworkDataManager.Instance.GetAvatarUser(i);
            if (avt == null)
            {
                avt = defaultAvatar;
            }
            var img = CreateImage(avt);
            container.Add(i, img);
        }
        onFriendAdded = new EventBinding<DataEvent.OnFriendAdded>(OnFriendAdded);
        onFriendRemoved = new EventBinding<DataEvent.OnFriendRemoved>(OnFriendRemoved);
        EventBus<DataEvent.OnFriendAdded>.Register(onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Register(onFriendRemoved);
    }

    void OnFriendAdded(DataEvent.OnFriendAdded args)
    {
        if (container.ContainsKey(args.userID)) return;
        var sprite = NetworkDataManager.Instance.GetAvatarUser(args.userID);
        var img = CreateImage(sprite);
        container.Add(args.userID, img);
    }
    void OnFriendRemoved(DataEvent.OnFriendRemoved args)
    {
        if (container.ContainsKey(args.userID))
        {
            var img = container[args.userID];   
            container.Remove(args.userID);
            Destroy(img.gameObject);
        }
    }

    Image CreateImage(Sprite sprite)
    {
        Debug.Log("Create image sprite avt called");

        GameObject go = new GameObject("FriendAvatar");
        RectTransform rectTransform = go.AddComponent<RectTransform>();

        rectTransform.SetParent(Content, false);
        rectTransform.sizeDelta = new Vector2(130, 130);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        return img;
    }
}
