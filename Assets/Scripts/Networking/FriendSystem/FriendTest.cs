using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FriendTest : MonoBehaviour
{
    [SerializeField] TMP_InputField Request;
    FriendManager friendManager;

    private void Awake()
    {
        EventBinding<EventTest.OnLoginSuccess> bind = new EventBinding<EventTest.OnLoginSuccess>(OnLoginSuccess);
        EventBus<EventTest.OnLoginSuccess>.Register(bind);
    }

    private async void OnLoginSuccess(EventTest.OnLoginSuccess success)
    {
        friendManager = new FriendManager();
        await friendManager.Initialize();
    }

    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            friendManager.MakeFriend.AcceptFriendRequest(Request.text);
        }
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            friendManager.MakeFriend.DeclineFriendRequest(Request.text);
        }
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            friendManager.MakeFriend.SendFriendRequest(Request.text);
        }
    }  
    
}
