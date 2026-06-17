using UnityEngine;


[RequireComponent(typeof(Collider))]
public class InteractionController : MonoBehaviour
{
    PlayerInputController playerInput;
    IInteractor player;
    IInteractable curInteractable;
    float holdTime = 0;
    bool isHolding = false;


    private void Awake()
    {
        player = GetComponent<IInteractor>();
    }
    private void Update()
    {
        var input = playerInput.GetInput();
        if (input.Interact)
        {
            if (!isHolding)
            {
                isHolding = StartInteraction();
            }
            else
            {
                isHolding = UpdateInteraction();
            }
        }else if (isHolding)
        {
            CancelInteraction();
        }
    }


    bool StartInteraction()
    {
        if (curInteractable == null) return false;
        if (curInteractable.HoldInteract)
        {
            if (curInteractable.HoldDuration <= 0)
            {
                curInteractable.Interact(player, InteractionPhase.Performed);
            }
            else
            {
                curInteractable.Interact(player, InteractionPhase.Started);
            }
            return true;
        }
        else
        {
            curInteractable.Interact(player, InteractionPhase.Performed);
            curInteractable = null;
            return false;
        }
       
    }
    bool UpdateInteraction()
    {
        if (curInteractable == null) return false;
        if (curInteractable.HoldDuration <= 0)
        {
            return true;
        }
        holdTime += Time.deltaTime;
        if (holdTime >= curInteractable.HoldDuration)
        {
            // Báo cho Object biết: "Tôi đã giữ đủ thời gian! Kích hoạt đi!"
            curInteractable.Interact(player, InteractionPhase.Performed);
            curInteractable = null;
            return false;
        }
        return true;
    }
     void CancelInteraction()
    {
        if (curInteractable != null)
        {
            // Báo cho Object biết: "Tôi thả tay ra rồi, hủy đi!"
            curInteractable.Interact(player, InteractionPhase.Canceled);
        }
        isHolding = false;
        curInteractable = null;
    }  

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
           this.curInteractable = interactable;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && curInteractable == interactable)
        {
            curInteractable = null;
        }
    }   
}
