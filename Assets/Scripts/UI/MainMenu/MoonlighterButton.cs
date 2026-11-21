using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class MoonlighterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.937f, 0.925f, 0.902f, 1f); // #EFECE6
    [SerializeField] private Color hoverColor = new Color(0.078f, 0.945f, 0.584f, 1f);  // #14F195
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.85f);          // Bóng tối tách nền

    [Header("Pop & Feel Settings")]
    [SerializeField] private float hoverScale = 1.1f;      // Độ phóng to khi nổi lên (1.08 - 1.12)
    [SerializeField] private float pressedScale = 0.96f;   // Độ lún nhẹ khi nhấn chuột xuống
    [SerializeField] private float duration = 0.16f;       // Thời gian chuyển động
    [SerializeField] private float shadowOffsetY = -0.35f; // Độ sâu bóng đổ bên dưới chữ

    private Coroutine animRoutine;
    private Material textMatInstance;
    private bool isHovered;

    private void Awake()
    {
        if (labelText == null) labelText = GetComponentInChildren<TextMeshProUGUI>();

        if (labelText != null)
        {
            // Tạo instance material riêng để bóng đổ của nút này không ảnh hưởng các chữ khác
            textMatInstance = labelText.fontMaterial;
            textMatInstance.EnableKeyword("UNDERLAY_ON");
            textMatInstance.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
            textMatInstance.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
        }

        ResetVisuals();
    }

    private void OnDisable()
    {
        isHovered = false;
        ResetVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        PlayTransition(hoverColor, hoverScale, 1f, useBounce: true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        PlayTransition(normalColor, 1f, 0f, useBounce: false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        PlayTransition(hoverColor, pressedScale, 0.3f, useBounce: false, customDuration: 0.06f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isHovered)
            PlayTransition(hoverColor, hoverScale, 1f, useBounce: true, customDuration: 0.1f);
        else
            PlayTransition(normalColor, 1f, 0f, useBounce: false, customDuration: 0.1f);
    }

    private void ResetVisuals()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (labelText == null) return;

        labelText.color = normalColor;
        labelText.rectTransform.localScale = Vector3.one;
        ApplyShadow(0f);
    }

    private void PlayTransition(Color targetCol, float targetScale, float targetShadowWeight, bool useBounce, float customDuration = -1f)
    {
        if (labelText == null) return;
        if (animRoutine != null) StopCoroutine(animRoutine);
        float animTime = customDuration > 0f ? customDuration : duration;
        animRoutine = StartCoroutine(AnimateRoutine(targetCol, targetScale, targetShadowWeight, useBounce, animTime));
    }

    private IEnumerator AnimateRoutine(Color targetCol, float targetScale, float targetShadowWeight, bool useBounce, float animTime)
    {
        float elapsed = 0f;
        Color startCol = labelText.color;
        float startScale = labelText.rectTransform.localScale.x;
        float startShadow = (textMatInstance != null && shadowColor.a > 0.001f)
            ? textMatInstance.GetColor(ShaderUtilities.ID_UnderlayColor).a / shadowColor.a
            : 0f;

        while (elapsed < animTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(elapsed / animTime);

            float t = useBounce ? EaseOutBack(rawT) : Mathf.SmoothStep(0f, 1f, rawT);

            labelText.color = Color.Lerp(startCol, targetCol, Mathf.Clamp01(rawT * 1.25f));
            float currentScale = Mathf.LerpUnclamped(startScale, targetScale, t);
            labelText.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

            ApplyShadow(Mathf.Lerp(startShadow, targetShadowWeight, rawT));

            yield return null;
        }

        labelText.color = targetCol;
        labelText.rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
        ApplyShadow(targetShadowWeight);
    }

    private void ApplyShadow(float weight)
    {
        if (textMatInstance == null) return;

        Color c = shadowColor;
        c.a = shadowColor.a * Mathf.Clamp01(weight);
        textMatInstance.SetColor(ShaderUtilities.ID_UnderlayColor, c);
        textMatInstance.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, shadowOffsetY * weight);
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}