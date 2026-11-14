using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntValueInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Slider slider;

    [Header("Range")]
    [SerializeField] private int minValue = 0;
    [SerializeField] private int maxValue = 100;

    public event Action<int> OnValueChanged;

    public int Value { get; private set; }

    private bool _isUpdating;

    private void Awake()
    {
        slider.wholeNumbers = true;
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        inputField.onValueChanged.AddListener(OnInputFieldChanged);
        slider.onValueChanged.AddListener(OnSliderChanged);

        SetValue(minValue, true);
    }

    private void OnDestroy()
    {
        inputField.onEndEdit.RemoveListener(OnInputFieldChanged);
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    public void SetMinMaxValue(int min = 0, int max = 100)
    {
        minValue = min;
        maxValue = max;
    }


    private void OnInputFieldChanged(string text)
    {
        if (_isUpdating)
            return;

        if (!int.TryParse(text, out int value))
        {
            SetValue(Value, true);
            return;
        }

        SetValue(value);
    }

    private void OnSliderChanged(float value)
    {
        if (_isUpdating)
            return;

        SetValue(Mathf.RoundToInt(value));
    }

    public void SetValue(int value, bool forceRefresh = false)
    {
        value = Mathf.Clamp(value, minValue, maxValue);

        if (!forceRefresh && value == Value)
            return;

        Value = value;

        _isUpdating = true;

        inputField.text = Value.ToString();
        slider.value = Value;

        _isUpdating = false;

        OnValueChanged?.Invoke(Value);
    }
}