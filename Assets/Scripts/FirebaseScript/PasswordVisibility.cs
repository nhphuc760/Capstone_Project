using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordVisibility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_InputField targetInput;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        // Mặc định ẩn mật khẩu
        toggle.isOn = false;
        SetPasswordVisible(false);

        toggle.onValueChanged.AddListener(SetPasswordVisible);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(SetPasswordVisible);
    }

    private void SetPasswordVisible(bool isVisible)
    {
        if (targetInput == null)
            return;

        targetInput.contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        targetInput.ForceLabelUpdate();
    }
}