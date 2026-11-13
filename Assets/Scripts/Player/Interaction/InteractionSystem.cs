using System.Text;
using Fusion;
using Newtonsoft.Json;
using UnityEngine;

public class InteractionSystem : NetworkBehaviour, IBeforeTick
{
    [Header("Interaction")]
    [SerializeField] float defaultInteractRange = 1f;
    [SerializeField] SuggestTarget suggestTarget;

    // ===== Networked State =====
    [Networked] public NetworkBool IsInteracting { get; set; }
    [Networked] float HoldTime { get; set; }

    [SerializeField] NetworkPlayer player;
    [SerializeField] Animator animator;

    // ===== Client-only =====
    IInteractable _currentInteractable;


    //Events
    EventBinding<ReliableReceiveData> reliableEvent;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            reliableEvent = new EventBinding<ReliableReceiveData>(OnReliableReceiveData);
            EventBus<ReliableReceiveData>.Register(reliableEvent);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            EventBus<ReliableReceiveData>.Deregister(reliableEvent);
        }
    }

    void OnReliableReceiveData(ReliableReceiveData data)
    {
        if (data.key == ConstReliableKey.InteractionResult)
        {
            string json = Encoding.UTF8.GetString(data.data);
            InteractionResult result = JsonConvert.DeserializeObject<InteractionResult>(json);
            if (result != null)
            {
                NotificationManager.Ins.ShowNotification(result.Message, result.Success ? 1 : 2);
            }
        }
    }

    public void BeforeTick()
    {        
        if(IsInteracting && suggestTarget.CurrentTarget == null)
        {
            SendPhase(_currentInteractable, InteractPhase.Cancel);
        }
        _currentInteractable = suggestTarget.CurrentTarget != null ? suggestTarget.CurrentTarget.GetComponent<IInteractable>() : null;
    }

    // Server xử lý Continuous        
    public void Pressed()
    {
        if(_currentInteractable != null && (_currentInteractable.InteractTransform.position - player.transform.position).sqrMagnitude < defaultInteractRange * defaultInteractRange)
        {
            var result = _currentInteractable.CanInteract(player);
            if (result.Success)
            {
                IsInteracting = true;
                HoldTime = 0;
                if(_currentInteractable.OnCompleted == null)
                    _currentInteractable.OnCompleted += OnCompleted;              
                //Interactor play animation _currentInteractable.Data(player).InteractorAnimation after delay
                string animState = _currentInteractable.Data(player)?.InteractorAnimation.stateName;
                if(!string.IsNullOrEmpty(animState))
                {
                    animator.Play(animState);
                    float speed = player._stats.Get(StatsType.AttackSpeed) / _currentInteractable.Data(player).InteractorAnimation.hzState;
                    animator.speed = speed;                   
                }
                //Lockat target
                Vector3 dir = (_currentInteractable.InteractTransform.position - player.transform.position).normalized;
                dir.y = 0f;
                player.transform.GetChild(0).localRotation = Quaternion.LookRotation(dir);
                SendPhase(_currentInteractable, InteractPhase.Press);
            }           
                string json = JsonConvert.SerializeObject(result);
                byte[] sData = Encoding.UTF8.GetBytes(json);
                Runner.SendReliableDataToPlayer(Object.InputAuthority, ConstReliableKey.InteractionResult, sData);            
        }        
    }
    public void Hold(float deltaTime)
    {
        if (IsInteracting) 
        {
            HoldTime += deltaTime;
            SendPhase(_currentInteractable, InteractPhase.Hold);
        }
    }

    public void Released()
    {       
        if (IsInteracting)
        {
            IsInteracting = false;
            SendPhase(_currentInteractable, InteractPhase.Release);
            player.GetComponentInChildren<Animator>().Play("Idle");
        }
    }


    void OnCompleted()
    {
        IsInteracting = false;
        animator.Play("Idle");
        animator.speed = 1f;
    }  

    private void SendPhase(
       IInteractable interactable,
       InteractPhase phase)
    {
        interactable?.OnInteract(
            new InteractionContext
            {
                Phase = phase,
                HoldTime = HoldTime,
                Interactor = player
            });
    }

}
