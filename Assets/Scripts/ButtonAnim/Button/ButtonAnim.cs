using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Fill Animation")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Color fillColor = new Color(1f, 0.78f, 0.18f, 1f);
    [SerializeField] private float enterDuration = 0.25f;
    [SerializeField] private float exitDuration = 0.18f;
    [SerializeField] private Ease enterEase = Ease.OutCubic; // dùng để tạo hiệu ứng mượt mà khi con trỏ chuột di chuyển vào nút
    [SerializeField] private Ease exitEase = Ease.InCubic; // dùng để tạo hiệu ứng mượt mà khi con trỏ chuột di chuyển ra khỏi nút
    [SerializeField] private GameObject Logo;

    private Tween fillTween;

    private void Awake()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        SetupFillImage();
        Logo.SetActive(false); 
    }

    private void OnEnable()
    {
        SetFillAmount(0f);
    }

    private void OnDisable()
    {
        fillTween?.Kill();
        fillTween = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayFillAnimation(1f, enterDuration, enterEase);
        Logo.SetActive(true); // Hiển thị Logo khi con trỏ chuột di chuyển vào nút
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayFillAnimation(0f, exitDuration, exitEase);
        Logo.SetActive(false); // Ẩn Logo khi con trỏ chuột di chuyển ra khỏi nút
    }

    private void SetupFillImage()
    {
        if (fillImage == null)
        {
            Debug.LogWarning($"{nameof(ButtonAnim)} needs an Image to play the fill animation.", this);
            return;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;
        SetFillAmount(0f);
    }

    private void PlayFillAnimation(float targetAmount, float duration, Ease ease)
    {
        if (fillImage == null)
        {
            return;
        }

        fillTween?.Kill();
        fillTween = fillImage.DOFillAmount(targetAmount, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void SetFillAmount(float amount)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = amount;
        }
    }
}
