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
        currentInvite = args.invite;
    }

    private void OnLoginSuccess()
    {
        inviteManager = new InviteManager(FirebaseManager.UserID);
    }

    private async void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            inviteManager.SendInvite(userIDInvite.text);
        }
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if(currentInvite != null)
            {
                currentInvite.Status = InviteStatus.Accepted;
                await inviteManager.RepplyInvite(currentInvite.SenderID, currentInvite);
            }
        }
    }
}
