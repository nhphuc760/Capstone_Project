using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class InvitePanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    Dictionary<string, InviteElementUI> container = new();
    EventBinding<InviteEvent.OnAddInviteArgs> onAddedInvite;
    EventBinding<InviteEvent.OnUpdateInviteArgs> onUpdateInvite;
    EventBinding<InviteEvent.OnRemoveInviteArgs> onRemoveInvite;
    const string INVITEELEMENT_KEY = "INVITEELEMENT";
    private void Awake()
    {
        onAddedInvite = new EventBinding<InviteEvent.OnAddInviteArgs>(OnInviteAdded);
        onUpdateInvite = new EventBinding<InviteEvent.OnUpdateInviteArgs>(OnInviteUpdate);
        onRemoveInvite = new EventBinding<InviteEvent.OnRemoveInviteArgs>(OnInviteRemove);
        Subcribe();
    }


    async void OnInviteAdded(InviteEvent.OnAddInviteArgs args)
    {
        if (!container.ContainsKey(args.senderID))
        {
            var inviteElement = ObjectPoolManager.Ins.Get(INVITEELEMENT_KEY, content).GetComponent<InviteElementUI>();
            Presence senderPresence = NetworkDataManager.Instance.GetPresenceUser(args.senderID);
            Sprite avt = NetworkDataManager.Instance.GetAvatarUser(args.senderID);
            string title = (senderPresence != null ? senderPresence.Name : null) + " đã gửi lời mời vào trận";
            container.Add(args.senderID, inviteElement);
            var result = await inviteElement.Show(title, avt);           
            await  NetworkDataManager.Instance.inviteManager.RepplyInvite(args.senderID, result ? InviteStatus.Accepted : InviteStatus.Rejected);
            
        }
    }

    void OnInviteUpdate(InviteEvent.OnUpdateInviteArgs args)
    {
        if (container.TryGetValue(args.senderID, out var invite))
        {
            invite.UpdateInvite();
            invite.transform.SetAsFirstSibling();
        }
    }
    void OnInviteRemove(InviteEvent.OnRemoveInviteArgs args)
    {

    }

    private void OnDestroy()
    {
        Desubcribe();
    }

    void Subcribe()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Register(onAddedInvite);
        EventBus<InviteEvent.OnUpdateInviteArgs>.Register(onUpdateInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Register(onRemoveInvite);
    }

    void Desubcribe()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Deregister(onAddedInvite);
        EventBus<InviteEvent.OnUpdateInviteArgs>.Deregister(onUpdateInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Deregister(onRemoveInvite);
    }



}
