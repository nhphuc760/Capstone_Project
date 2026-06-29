using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InviteSystemTest : MonoBehaviour
{
    InviteManager inviteManager;
    [SerializeField] TMP_InputField userIDInvite;

    private void Awake()
    {
        EventBinding<EventTest.OnLoginSuccess> test = new EventBinding<EventTest.OnLoginSuccess>(OnLoginSuccess);
        EventBus<EventTest.OnLoginSuccess>.Register(test);
    }
    private void OnLoginSuccess()
    {
        inviteManager = new InviteManager(FirebaseManager.UserID);
    }

    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            inviteManager.SendInvite(userIDInvite.text);
        }
    }
}
