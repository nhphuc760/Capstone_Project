using System.Collections.Generic;
using UnityEngine;

public class FriendUIManager : MonoBehaviour
{
    EventBinding<OnAddFriendArgs> onAddFriendBinding;
    [SerializeField] ElementFriendUI elementFriendPrefab;
    [SerializeField] RectTransform container;
    private void Awake()
    {

    }

    private void OnDestroy()
    {
        EventBus<OnAddFriendArgs>.Deregister(onAddFriendBinding);
    }
}
