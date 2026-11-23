using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InviteNotificationManager : MonoBehaviour
{
    [SerializeField] private RectTransform inviteContainer;
    [SerializeField] private Sprite defaultAvatar;

    private const string INVITE_ELEMENT_KEY = "INVITEELEMENT";
    private readonly Dictionary<string, InviteElementUI> _activeElements = new();

    private EventBinding<InviteEvent.OnAddInviteArgs> _onAddInvite;
    private EventBinding<InviteEvent.OnRemoveInviteArgs> _onRemoveInvite;

    private void Awake()
    {
        if (inviteContainer == null)
            inviteContainer = GetComponent<RectTransform>();

        _onAddInvite = new EventBinding<InviteEvent.OnAddInviteArgs>(args => HandleAddInviteAsync(args.senderID).Forget());
        _onRemoveInvite = new EventBinding<InviteEvent.OnRemoveInviteArgs>(args => HandleRemoveInvite(args.senderID));
    }

    private void OnEnable()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Register(_onAddInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Register(_onRemoveInvite);
    }

    private void OnDisable()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Deregister(_onAddInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Deregister(_onRemoveInvite);
    }

    private async UniTaskVoid HandleAddInviteAsync(string senderID)
    {
        if (string.IsNullOrEmpty(senderID) || _activeElements.ContainsKey(senderID))
            return;

        if (NetworkDataManager.Instance == null || ObjectPoolManager.Ins == null)
            return;

        await NetworkDataManager.Instance.LoadPresence(senderID);
        Presence senderPresence = NetworkDataManager.Instance.GetPresenceUser(senderID);
        Sprite senderAvatar = NetworkDataManager.Instance.GetAvatarUser(senderID);

        string senderName = senderPresence != null && !string.IsNullOrEmpty(senderPresence.Name)
            ? senderPresence.Name
            : "A friend";

        GameObject go = ObjectPoolManager.Ins.Get(INVITE_ELEMENT_KEY, inviteContainer, false);
        if (go == null) return;

        InviteElementUI element = go.GetComponent<InviteElementUI>();
        if (element == null) return;

        _activeElements[senderID] = element;

        bool accepted = await element.Show($"{senderName} invited you to Lobby", senderAvatar != null ? senderAvatar : defaultAvatar);

        _activeElements.Remove(senderID);

        if (NetworkDataManager.Instance?.inviteManager != null)
        {
            await NetworkDataManager.Instance.inviteManager.RepplyInvite(
                senderID,
                accepted ? InviteStatus.Accepted : InviteStatus.Rejected
            );
        }
    }

    private void HandleRemoveInvite(string senderID)
    {
        if (_activeElements.TryGetValue(senderID, out InviteElementUI element))
        {
            _activeElements.Remove(senderID);
            if (element != null && element.gameObject.activeSelf)
            {
                ObjectPoolManager.Ins.Release(INVITE_ELEMENT_KEY, element.gameObject);
            }
        }
    }
}