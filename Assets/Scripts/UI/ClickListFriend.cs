using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickListFriend : MonoBehaviour, IPointerClickHandler
{
    UIEvent.OnClickFriendList args = new UIEvent.OnClickFriendList();
    EventBinding<UIEvent.CloseFriendList> onCloseFriendList;
    private void Awake()
    {
        onCloseFriendList = new EventBinding<UIEvent.CloseFriendList>(FriendListClose);
        EventBus<UIEvent.CloseFriendList>.Register(onCloseFriendList);
    }  

    public void OnPointerClick(PointerEventData eventData)
    {
        Hide();
        EventBus<UIEvent.OnClickFriendList>.Raise(args);      
    }

    void FriendListClose(UIEvent.CloseFriendList args)
    {
        Show();
    }

    void Show()
    {
        transform.parent.gameObject.SetActive(true);
    }
    void Hide()
    {
        transform.parent.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        EventBus<UIEvent.CloseFriendList>.Deregister(onCloseFriendList);
    }
}
