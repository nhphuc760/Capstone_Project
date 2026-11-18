using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("UI Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 1f;

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_selectable.interactable) return;
        AudioManager.Instance?.PlayUI(hoverClip, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!_selectable.interactable) return;

        AudioManager.Instance?.PlayUI(clickClip, clickVolume);
    }
}