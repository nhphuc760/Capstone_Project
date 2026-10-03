using UnityEngine;

public class BuildUI : MonoBehaviour, IPlayerUI
{
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    private void Awake()
    {
        GetComponentInParent<PlayerUIComponent>().RegisterPlayerUI(PlayerUIComponent.OpenUI.BuildMenu, this);
        gameObject.SetActive(false);
    }
}
