using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] FriendRequestUI frequestUIPrefabs;
    [SerializeField] Image announceIcon;
    private void Awake()
    {
        var list = NetworkDataManager.Instance.friendManager.MakeFriend.GetFriendRequests();
        if (list == null || list.Count == 0) return;
        foreach (var i in list)
        {
            InstantiateElement(i);
        }
    }

    private void Start()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.onChildAdded += InstantiateElement;
    }

    public void UpdateAnnouce()
    {        
       announceIcon.gameObject.SetActive(transform.childCount > 0);        
    }

    void InstantiateElement(string userID)
    {
        var obj = Instantiate(frequestUIPrefabs, content);
        obj.UpdateUI(userID);
    }
    private void OnDestroy()
    {
        NetworkDataManager.Instance.friendManager.MakeFriend.onChildAdded -= InstantiateElement;
    }
}
