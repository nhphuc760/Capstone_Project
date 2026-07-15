using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InviteSystemTest : MonoBehaviour
{
    InviteManager inviteManager;
    [SerializeField] TMP_InputField userIDInvite;
    Invite currentInvite;

    private void Awake()
    {
        //EventBinding<EventTest.OnLoginSuccess> test = new EventBinding<EventTest.OnLoginSuccess>(OnLoginSuccess);
        EventBinding<InviteEvent.OnAddInviteArgs> onChildAddedInvite = new EventBinding<InviteEvent.OnAddInviteArgs>(OnAddInvite);
        //EventBus<EventTest.OnLoginSuccess>.Register(test);
        EventBus<InviteEvent.OnAddInviteArgs>.Register(onChildAddedInvite);
    }


    private void OnAddInvite(InviteEvent.OnAddInviteArgs args)
    {
    }

    private void OnLoginSuccess()
    {
        inviteManager = new InviteManager(FirebaseManager.UserID);
    }
   
}
