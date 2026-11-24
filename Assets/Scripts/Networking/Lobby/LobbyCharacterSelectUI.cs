using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCharacterSelectUI : MonoBehaviour
{
    [Header("Dữ liệu & Nơi chứa nút")]
    [SerializeField] private CharacterDatabase database;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button characterSlotPrefab;

    [Header("Màu viền hoặc nền khi được chọn")]
    [SerializeField] private Color selectedColor = new Color32(74, 217, 145, 255);
    [SerializeField] private Color normalColor = new Color32(44, 53, 64, 255);

    private readonly List<Image> _slotBackgrounds = new();
    private LobbyCharacterSelector _localSelector;
    private int _currentSelectedIndex = -1;

    private void Start()
    {
        if (database == null)
            database = Resources.Load<CharacterDatabase>("CharacterDatabase");

        BuildCharacterSlots();

        int savedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        UpdateHighlight(savedIndex);
    }

    private void BuildCharacterSlots()
    {
        if (database == null || buttonContainer == null || characterSlotPrefab == null) return;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        _slotBackgrounds.Clear();

        for (int i = 0; i < database.characters.Count; i++)
        {
            int index = i;
            CharacterVisualData data = database.characters[i];

            Button slotBtn = Instantiate(characterSlotPrefab, buttonContainer);
            slotBtn.gameObject.SetActive(true);

            Image bg = slotBtn.GetComponent<Image>();
            if (bg != null) _slotBackgrounds.Add(bg);

            // Gán AvatarIcon nếu nút có Image con tên "Icon"
            Transform iconTr = slotBtn.transform.Find("Icon");
            if (iconTr != null && iconTr.TryGetComponent<Image>(out var iconImg) && data.avatarIcon != null)
            {
                iconImg.sprite = data.avatarIcon;
            }

            // Gán tên nhân vật nếu nút có TextMeshProUGUI con
            var label = slotBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(data.characterName) ? $"Char {i + 1}" : data.characterName;
            }

            slotBtn.onClick.AddListener(() => OnSelectCharacter(index));
        }
    }

    public void OnSelectCharacter(int index)
    {
        if (_localSelector == null || _localSelector.Object == null || !_localSelector.Object.IsValid)
        {
            foreach (var selector in FindObjectsByType<LobbyCharacterSelector>(FindObjectsSortMode.None))
            {
                if (selector.Object != null && selector.Object.IsValid && selector.Object.HasInputAuthority)
                {
                    _localSelector = selector;
                    break;
                }
            }
        }

        if (_localSelector != null)
        {
            _localSelector.SelectCharacter(index);
            UpdateHighlight(index);
        }
    }

    private void UpdateHighlight(int selectedIndex)
    {
        _currentSelectedIndex = selectedIndex;
        for (int i = 0; i < _slotBackgrounds.Count; i++)
        {
            if (_slotBackgrounds[i] != null)
                _slotBackgrounds[i].color = (i == _currentSelectedIndex) ? selectedColor : normalColor;
        }
    }
}