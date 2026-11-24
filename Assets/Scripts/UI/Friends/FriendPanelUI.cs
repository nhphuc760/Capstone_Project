using System.Collections.Generic;
using UnityEngine;

public class FriendPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] FriendElementUI elementFriendPrefabs;

    private readonly Dictionary<string, FriendElementUI> container = new();
    private EventBinding<DataEvent.OnFriendAdded> onFriendAdded;
    private EventBinding<DataEvent.OnFriendRemoved> onFriendRemoved;

    private void Awake()
    {
        Initialize(NetworkDataManager.Instance.friendManager.GetFriends());
        onFriendAdded = new EventBinding<DataEvent.OnFriendAdded>(OnFriendAddedHandle);
        onFriendRemoved = new EventBinding<DataEvent.OnFriendRemoved>(OnFriendRemoveHandle);
        SubcribeEvent();
    }

    private void OnEnable()
    {
        Refesh();
    }

    private void OnFriendAddedHandle(DataEvent.OnFriendAdded args) => CreateElement(args.userID);
    private void OnFriendRemoveHandle(DataEvent.OnFriendRemoved args) => RemoveElement(args.userID);

    private void OnFriendPresenceChanged(string userID)
    {
        if (container.TryGetValue(userID, out var user) && user != null)
            user.UpdateUI();
    }

    private void Initialize(List<string> listFriends)
    {
        foreach (var id in listFriends)
            CreateElement(id);
    }

    public void Refesh()
    {
        foreach (var item in container.Values)
            if (item != null) item.UpdateUI();
    }

    private void CreateElement(string userID)
    {
        if (string.IsNullOrEmpty(userID) || container.ContainsKey(userID)) return;

        var ele = Instantiate(elementFriendPrefabs, content);
        ele.Init(userID);
        container.Add(userID, ele);
    }

    private void RemoveElement(string userID)
    {
        if (container.TryGetValue(userID, out FriendElementUI ele))
        {
            container.Remove(userID);
            if (ele != null)
                Destroy(ele.gameObject);
        }
    }

    private void OnDestroy() => DesubcribeEvent();

    private void SubcribeEvent()
    {
        NetworkDataManager.Instance.friendManager.OnPresenceChanged += OnFriendPresenceChanged;
        EventBus<DataEvent.OnFriendAdded>.Register(onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Register(onFriendRemoved);
    }

    private void DesubcribeEvent()
    {
        if (NetworkDataManager.Instance?.friendManager != null)
            NetworkDataManager.Instance.friendManager.OnPresenceChanged -= OnFriendPresenceChanged;

        EventBus<DataEvent.OnFriendAdded>.Deregister(onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Deregister(onFriendRemoved);
    }
}