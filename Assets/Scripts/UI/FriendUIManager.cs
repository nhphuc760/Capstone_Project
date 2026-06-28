using System.Collections.Generic;
using UnityEngine;

public class FriendUIManager : MonoBehaviour
{
    EventBinding<OnAddFriendArgs> onAddFriendBinding;
    [SerializeField] ElementFriendUI elementFriendPrefab;
    [SerializeField] RectTransform container;
    private void Awake()
    {
        InitListFriendUI(NetworkDataManager.Instance.friendManager.listFriends);
    }

    void InitListFriendUI(Dictionary<string, Presence> listFriend)
    {
        foreach (var i in listFriend)
        {
            var elementUI = Instantiate(elementFriendPrefab, container);
            var avatar = NetworkDataManager.Instance.friendManager.GetAvatar(i.Key);
            elementUI.SetUp(i.Value, avatar, () => NetworkDataManager.Instance.inviteManager.SendInvite(i.Key));
        }
    }
    private void Start()
    {

        onAddFriendBinding = new EventBinding<OnAddFriendArgs>(OnAddFriend);
        EventBus<OnAddFriendArgs>.Register(onAddFriendBinding);
    }




    void OnAddFriend(OnAddFriendArgs args)
    {
        ElementFriendUI element = Instantiate(elementFriendPrefab, container);
        element.SetUp(args.presence, args.avatar, () => NetworkDataManager.Instance.inviteManager.SendInvite(args.UserID));
    }
    private void OnDestroy()
    {
        EventBus<OnAddFriendArgs>.Deregister(onAddFriendBinding);
    }
}
