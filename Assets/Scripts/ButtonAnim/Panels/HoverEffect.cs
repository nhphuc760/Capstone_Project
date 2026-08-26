using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;

    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color hoverColor = new Color(0f, 0f, 0f, 1f);

    [SerializeField] private float duration = 0f;

    private Tween colorTween;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        targetImage.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        colorTween?.Kill();
        colorTween = targetImage.DOColor(hoverColor, duration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        colorTween?.Kill();
        colorTween = targetImage.DOColor(normalColor, duration);
    }

    private void OnDisable()
    {
        colorTween?.Kill();
    }
}