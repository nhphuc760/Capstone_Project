using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UIOutline = UnityEngine.UI.Outline;

[DisallowMultipleComponent]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hiệu ứng nổi lên (Scale)")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float transitionSpeed = 14f;

    [Header("Hiệu ứng viền xanh (Outline)")]
    [SerializeField] private Color borderColor = new Color32(92, 224, 179, 255); // Xanh ngọc #5CE0B3
    [SerializeField] private Vector2 borderThickness = new Vector2(2.5f, -2.5f);
    [SerializeField] private bool hideBorderWhenIdle = true;

    [Header("Làm sáng nền nhẹ khi Hover")]
    [SerializeField] private bool brightenOnHover = true;
    [SerializeField] private float hoverBrightness = 1.12f;

    private Selectable selectable;
    private Graphic targetGraphic;
    private UIOutline outline;

    private Vector3 initialScale;
    private Color initialGraphicColor;
    private bool isHovered;
    private bool isPressed;
    private bool isInitialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        selectable = GetComponent<Selectable>();
        targetGraphic = selectable != null ? selectable.targetGraphic : GetComponent<Graphic>();
        initialScale = transform.localScale;

        if (targetGraphic != null)
        {
            initialGraphicColor = targetGraphic.color;

            // Tự lấy UnityEngine.UI.Outline có sẵn hoặc gắn mới nếu Button chưa có
            outline = targetGraphic.GetComponent<UIOutline>();
            if (outline == null)
                outline = targetGraphic.gameObject.AddComponent<UIOutline>();

            outline.effectDistance = borderThickness;
            outline.useGraphicAlpha = false;

            Color startBorder = borderColor;
            startBorder.a = hideBorderWhenIdle ? 0f : borderColor.a * 0.35f;
            outline.effectColor = startBorder;
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        ResetVisualImmediate();
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        ResetVisualImmediate();
    }

    private void Update()
    {
        bool canInteract = selectable == null || selectable.interactable;
        if (!canInteract)
        {
            isHovered = false;
            isPressed = false;
        }

        float dt = Time.unscaledDeltaTime * transitionSpeed;

        // 1. Mượt hóa độ phóng to (nổi lên)
        float targetScaleFactor = 1f;
        if (canInteract)
        {
            if (isPressed) targetScaleFactor = pressedScale;
            else if (isHovered) targetScaleFactor = hoverScale;
        }
        transform.localScale = Vector3.Lerp(transform.localScale, initialScale * targetScaleFactor, dt);

        // 2. Mượt hóa viền màu xanh
        if (outline != null)
        {
            Color targetBorder = borderColor;
            if (!isHovered || !canInteract)
                targetBorder.a = hideBorderWhenIdle ? 0f : borderColor.a * 0.35f;

            outline.effectColor = Color.Lerp(outline.effectColor, targetBorder, dt);
        }

        // 3. Làm sáng nhẹ màu nền nút khi đưa chuột vào
        if (brightenOnHover && targetGraphic != null)
        {
            Color targetColor = (isHovered && canInteract)
                ? MultiplyColorRGB(initialGraphicColor, hoverBrightness)
                : initialGraphicColor;

            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, dt);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void ResetVisualImmediate()
    {
        transform.localScale = initialScale;

        if (outline != null)
        {
            Color idleBorder = borderColor;
            idleBorder.a = hideBorderWhenIdle ? 0f : borderColor.a * 0.35f;
            outline.effectColor = idleBorder;
        }

        if (targetGraphic != null)
            targetGraphic.color = initialGraphicColor;
    }

    private static Color MultiplyColorRGB(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a
        );
    }
}