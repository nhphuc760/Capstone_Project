using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementFriendUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _name;
    [SerializeField] TextMeshProUGUI tags;
    [SerializeField] TextMeshProUGUI status;
    [SerializeField] Image avatar;
    [SerializeField] Button invite;
    User _friendData;
    public void SetUp(User user, Action callbackInvite = default)
    {
        _friendData = user;
        invite.onClick.AddListener(() => 
        {
            callbackInvite.Invoke();
        });
        //UpdateUI
    }
    public void UpdateUI(User user)
    {
        if (_friendData != null)
        {
            Debug.Log("UserData is null");
            return;            
        }
        _name.text = user.Name;
        tags.text = user.Tag;
        status.text = user.Status.ToString();
    }    
}
