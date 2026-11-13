using System;
using Fusion;
using UnityEngine;

public class SnowmanNPC : MonoBehaviour, IInteractable
{
    [SerializeField] ShopManager Shop;
    [SerializeField] GameObject ShopUI;
    [SerializeField] NetworkObject networkObj;
    public Transform InteractTransform =>transform;
    public Action OnCompleted { get; set; }


    private void Start()
    {

    }



    private void OnDestroy()
    {
        
    }

    public InteractionResult CanInteract(NetworkPlayer interactor)
    {
        if (!Shop.IsValid(interactor.Object.InputAuthority))
        {
            Debug.Log("Bị cấm giao thương", this);
            return new InteractionResult(false, "Bạn bị cấm giao thương cho tới round sau");
        }
        return new InteractionResult(true);
    }

    public InteractionData Data(NetworkPlayer interactor) => null;    

    public void OnInteract(InteractionContext ctx)
    {
        if (networkObj.Runner.LocalPlayer != ctx.Interactor.Object.InputAuthority) return;
        ShowUI(true);
        OnCompleted?.Invoke();
    }

    public void ShowUI(bool value)
    {
        ShopUI?.SetActive(value);
    }

}
