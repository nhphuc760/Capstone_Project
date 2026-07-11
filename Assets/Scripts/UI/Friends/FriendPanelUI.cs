using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class FriendPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] FriendElementUI elementFriendPrefabs;
    Dictionary<string, FriendElementUI> container;


    EventBinding<DataEvent.OnFriendAdded> onFriendAdded;
    EventBinding<DataEvent.OnFriendRemoved> onFriendRemoved;
    private void Awake()
    {
        Initialize(NetworkDataManager.Instance.friendManager.GetFriends());
        onFriendAdded = new EventBinding<DataEvent.OnFriendAdded>(OnFriendAddedHandle);
        onFriendRemoved = new EventBinding<DataEvent.OnFriendRemoved>(OnFriendRemoveHandle);
        EventBus<DataEvent.OnFriendAdded>.Register(onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Register(onFriendRemoved);
    }
    
    void OnFriendAddedHandle(DataEvent.OnFriendAdded args)
    {
        CreateElement(args.userID);
    }
    void OnFriendRemoveHandle(DataEvent.OnFriendRemoved args)
    {
        RemoveElement(args.userID);
    }


    

    void Initialize(List<string> listFriends)
    {
        foreach (var i in listFriends)
        {
            CreateElement(i);
        }
    } 

    public void Refesh()
    {       
        foreach (var i in container.Values)
        {
            i.Refesh();
        }
    }

    void CreateElement(string userID)
    {
        var ele = Instantiate(elementFriendPrefabs, content);
        ele.Init(userID);
        container.Add(userID, ele);
    }
    void RemoveElement(string userID)
    {
        if (container.TryGetValue(userID, out FriendElementUI ele))
        {
            Destroy(ele.gameObject);
        }
    }
    private void OnDestroy()
    {
        EventBus<DataEvent.OnFriendAdded>.Deregister(onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Deregister(onFriendRemoved);
    }
}
