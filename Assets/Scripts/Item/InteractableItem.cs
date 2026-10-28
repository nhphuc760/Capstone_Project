using UnityEngine;
using Fusion;
[RequireComponent(typeof(Outline))]
public class InteractableItem : NetworkBehaviour
{
    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    public void ToggleHighlight(bool isGlowing)
    {
        if (outline != null)
        {
            outline.enabled = isGlowing;
        }
    }

    public void PickUpItem()
    {
        // viết script để lụm vật phẩm, ví dụ: thêm vào inventory của người chơi sau này

        Debug.Log("Đã lụm thành công vật phẩm: " + gameObject.name);

        if (Object != null && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
