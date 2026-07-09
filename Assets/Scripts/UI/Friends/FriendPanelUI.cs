using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class FriendPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] FriendElementUI elementFriendPrefabs;
    private void Awake()
    {
        Initialize(NetworkDataManager.Instance.friendManager.GetFriends());
    }

    void Initialize(List<string> listFriends)
    {
        foreach (var i in listFriends)
        {
            var ele = Instantiate(elementFriendPrefabs, content);
            ele.Init(i);
        }
    } 

    public void Refesh()
    {
        for (int i = 0; i< content.childCount; i++)
        {
            content.GetChild(i).GetComponent<FriendElementUI>().Refesh();
        }
    }
}
