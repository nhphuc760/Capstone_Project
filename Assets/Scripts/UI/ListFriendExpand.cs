using UnityEngine;

public class ListFriendExpand : MonoBehaviour
{
    UIEvent.CloseFriendList args = new UIEvent.CloseFriendList();
    EventBinding<UIEvent.OnClickFriendList> onClickFriendList;
    private void Awake()
    {
        onClickFriendList = new EventBinding<UIEvent.OnClickFriendList>(OnClickFriendList);
        EventBus<UIEvent.OnClickFriendList>.Register(onClickFriendList);
    }
    void Start()
    {
        Hide();
    }

    public void OnCloseList()
    {
        Hide();
        EventBus<UIEvent.CloseFriendList>.Raise(args);
    }

   void OnClickFriendList(UIEvent.OnClickFriendList args)
    {
        Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        EventBus<UIEvent.OnClickFriendList>.Deregister(onClickFriendList);
    }
}
