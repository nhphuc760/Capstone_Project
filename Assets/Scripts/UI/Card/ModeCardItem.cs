using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

public class ModeCardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum GameModeOption
    {
        SinglePlayer,
        HostCoop,
        JoinRoom
    }

    [Header("Mode Configuration")]
    public GameModeOption modeType;
    public string modeTitle = "Single Player";
    [TextArea(2, 4)] public string mainDescription = "Defend your fortress alone at your own pace.";
    [TextArea(2, 3)] public string highlightDescription = "The classic solo survival experience with full pause support.";

    [Header("Card Visual References")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image bgFillImage;
    [SerializeField] private Image borderGlowImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI cardNameText;

    [Header("Idle Colors")]
    [SerializeField] private Color idleBgColor = new Color(0.11f, 0.14f, 0.12f, 1f);      // #1C241E
    [SerializeField] private Color idleBorderColor = new Color(0.31f, 0.36f, 0.32f, 1f);  // #4E5C52
    [SerializeField] private Color idleContentColor = new Color(0.78f, 0.80f, 0.78f, 1f); // #C8CCC7

    [Header("Hover Glow Colors")]
    [SerializeField] private Color hoverBgColor = new Color(0.071f, 0.329f, 0.267f, 1f);     // #125444 (Xanh ngọc trầm)
    [SerializeField] private Color hoverBorderColor = new Color(0.361f, 0.878f, 0.702f, 1f); // #5CE0B3 (Viền ngọc dịu)
    [SerializeField] private Color hoverContentColor = new Color(0.949f, 0.937f, 0.914f, 1f);// #F2EFE9 (Trắng kem)

    [Header("Code-Driven Glow & Elevation")]
    [SerializeField] private float hoverScale = 1.07f;
    [SerializeField] private float floatOffsetY = 14f;
    [SerializeField] private float maxGlowSpread = 3.5f;    // Quầng sáng gọn, thanh thoát hơn
    [SerializeField] private float maxShadowDrop = -16f;
    [SerializeField] private float pulseSpeed = 3.5f;       // Nhịp thở chậm rãi, êm mắt
    [SerializeField] private float animDuration = 0.16f;

    public event Action<ModeCardItem> OnCardHovered;
    public event Action<ModeCardItem> OnCardClicked;

    private UnityEngine.UI.Outline borderOuterGlow;
    private UnityEngine.UI.Shadow cardElevationShadow;
    private Coroutine transitionRoutine;
    private bool isActiveGlow;
    private bool isSelectedLocked;

    private void Awake()
    {
        if (cardNameText != null) cardNameText.text = modeTitle;

        // Chỉ định rõ UnityEngine.UI.Outline để không trùng với asset Outline 3D
        if (borderGlowImage != null)
        {
            borderOuterGlow = borderGlowImage.GetComponent<UnityEngine.UI.Outline>();
            if (borderOuterGlow == null)
                borderOuterGlow = borderGlowImage.gameObject.AddComponent<UnityEngine.UI.Outline>();

            borderOuterGlow.useGraphicAlpha = false;
        }

        // Chỉ định rõ UnityEngine.UI.Shadow để tạo bóng đổ tách nền
        if (bgFillImage != null)
        {
            cardElevationShadow = bgFillImage.GetComponent<UnityEngine.UI.Shadow>();
            if (cardElevationShadow == null)
                cardElevationShadow = bgFillImage.gameObject.AddComponent<UnityEngine.UI.Shadow>();

            cardElevationShadow.useGraphicAlpha = false;
        }

        ApplyInstantState(false);
    }

    private void OnDisable()
    {
        isSelectedLocked = false;
        isActiveGlow = false;
        ApplyInstantState(false);
    }

    private void Update()
    {
        if (isActiveGlow && borderOuterGlow != null)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            Color pulseCol = hoverBorderColor;
            pulseCol.a = Mathf.Lerp(0.22f, 0.45f, wave);
            borderOuterGlow.effectColor = pulseCol;

            float spread = Mathf.Lerp(maxGlowSpread * 0.85f, maxGlowSpread * 1.15f, wave);
            borderOuterGlow.effectDistance = new Vector2(spread, -spread);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnCardHovered?.Invoke(this);
        AnimateToState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectedLocked)
            AnimateToState(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        OnCardClicked?.Invoke(this);
    }

    public void SetLockedHighlight(bool locked)
    {
        isSelectedLocked = locked;
        AnimateToState(locked);
    }

    private void ApplyInstantState(bool highlighted)
    {
        isActiveGlow = highlighted;

        if (visualRoot != null)
        {
            visualRoot.localScale = highlighted ? Vector3.one * hoverScale : Vector3.one;
            visualRoot.anchoredPosition = highlighted ? new Vector2(0f, floatOffsetY) : Vector2.zero;
        }

        if (bgFillImage != null) bgFillImage.color = highlighted ? hoverBgColor : idleBgColor;
        if (borderGlowImage != null) borderGlowImage.color = highlighted ? hoverBorderColor : idleBorderColor;
        if (iconImage != null) iconImage.color = highlighted ? hoverContentColor : idleContentColor;
        if (cardNameText != null) cardNameText.color = highlighted ? hoverContentColor : idleContentColor;

        if (borderOuterGlow != null)
        {
            Color glowCol = hoverBorderColor;
            glowCol.a = highlighted ? 0.55f : 0f;
            borderOuterGlow.effectColor = glowCol;
            borderOuterGlow.effectDistance = highlighted ? new Vector2(maxGlowSpread, -maxGlowSpread) : Vector2.zero;
        }

        if (cardElevationShadow != null)
        {
            cardElevationShadow.effectColor = new Color(0f, 0f, 0f, highlighted ? 0.88f : 0f);
            cardElevationShadow.effectDistance = highlighted ? new Vector2(0f, maxShadowDrop) : Vector2.zero;
        }
    }

    private void AnimateToState(bool toHover)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(AnimateRoutine(toHover));
    }

    private IEnumerator AnimateRoutine(bool toHover)
    {
        if (!toHover) isActiveGlow = false;

        float elapsed = 0f;

        Vector3 startScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        Vector3 targetScale = toHover ? Vector3.one * hoverScale : Vector3.one;

        Vector2 startPos = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
        Vector2 targetPos = toHover ? new Vector2(0f, floatOffsetY) : Vector2.zero;

        Color startBg = bgFillImage != null ? bgFillImage.color : idleBgColor;
        Color targetBg = toHover ? hoverBgColor : idleBgColor;

        Color startBorder = borderGlowImage != null ? borderGlowImage.color : idleBorderColor;
        Color targetBorder = toHover ? hoverBorderColor : idleBorderColor;

        Color startContent = cardNameText != null ? cardNameText.color : idleContentColor;
        Color targetContent = toHover ? hoverContentColor : idleContentColor;

        float startGlowAlpha = borderOuterGlow != null ? borderOuterGlow.effectColor.a : 0f;
        float targetGlowAlpha = toHover ? 0.55f : 0f;
        float startGlowDist = borderOuterGlow != null ? borderOuterGlow.effectDistance.x : 0f;
        float targetGlowDist = toHover ? maxGlowSpread : 0f;

        float startShadowAlpha = cardElevationShadow != null ? cardElevationShadow.effectColor.a : 0f;
        float targetShadowAlpha = toHover ? 0.88f : 0f;
        float startShadowY = cardElevationShadow != null ? cardElevationShadow.effectDistance.y : 0f;
        float targetShadowY = toHover ? maxShadowDrop : 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(elapsed / animDuration);
            float easeT = toHover ? EaseOutBack(rawT) : Mathf.SmoothStep(0f, 1f, rawT);

            if (visualRoot != null)
            {
                visualRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, easeT);
                visualRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, easeT);
            }

            if (bgFillImage != null) bgFillImage.color = Color.Lerp(startBg, targetBg, rawT);
            if (borderGlowImage != null) borderGlowImage.color = Color.Lerp(startBorder, targetBorder, rawT);
            if (iconImage != null) iconImage.color = Color.Lerp(startContent, targetContent, rawT);
            if (cardNameText != null) cardNameText.color = Color.Lerp(startContent, targetContent, rawT);

            if (borderOuterGlow != null)
            {
                Color gCol = hoverBorderColor;
                gCol.a = Mathf.Lerp(startGlowAlpha, targetGlowAlpha, rawT);
                borderOuterGlow.effectColor = gCol;

                float d = Mathf.Lerp(startGlowDist, targetGlowDist, easeT);
                borderOuterGlow.effectDistance = new Vector2(d, -d);
            }

            if (cardElevationShadow != null)
            {
                cardElevationShadow.effectColor = new Color(0f, 0f, 0f, Mathf.Lerp(startShadowAlpha, targetShadowAlpha, rawT));
                cardElevationShadow.effectDistance = new Vector2(0f, Mathf.Lerp(startShadowY, targetShadowY, easeT));
            }

            yield return null;
        }

        ApplyInstantState(toHover);
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}