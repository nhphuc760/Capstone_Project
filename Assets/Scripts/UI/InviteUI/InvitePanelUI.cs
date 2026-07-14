using System.Collections.Generic;
using UnityEngine;

public class InvitePanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    Dictionary<string, InviteElementUI> container = new();
    EventBinding<InviteEvent.OnAddInviteArgs> onAddedInvite;
    EventBinding<InviteEvent.OnUpdateInviteArgs> onUpdateInvite;
    EventBinding<InviteEvent.OnRemoveInviteArgs> onRemoveInvite;
    private void Awake()
    {
        onAddedInvite = new EventBinding<InviteEvent.OnAddInviteArgs>(OnInviteAdded);
        onUpdateInvite = new EventBinding<InviteEvent.OnUpdateInviteArgs>(OnInviteUpdate);
        onRemoveInvite = new EventBinding<InviteEvent.OnRemoveInviteArgs>(OnInviteRemove);
    }


    void OnInviteAdded(InviteEvent.OnAddInviteArgs args)
    {
        
    }

    void OnInviteUpdate(InviteEvent.OnUpdateInviteArgs args)
    {
        if (container.TryGetValue(args.senderID, out var invite))
        {

        }
    }
    void OnInviteRemove(InviteEvent.OnRemoveInviteArgs args)
    {

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
