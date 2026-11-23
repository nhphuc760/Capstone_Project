using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCharacterArrowUI : MonoBehaviour
{
    [Header("Dữ liệu nhân vật")]
    [SerializeField] private CharacterDatabase database;

    [Header("Tham chiếu UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI indexCounterText; // Tùy chọn: hiện "1 / 3"

    private LobbyCharacterSelector _localSelector;
    private int _currentIndex;

    private void Awake()
    {
        if (database == null)
            database = Resources.Load<CharacterDatabase>("CharacterDatabase");
    }

    private void Start()
    {
        _currentIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);

        if (database != null && database.characters.Count > 0)
            _currentIndex = Mathf.Clamp(_currentIndex, 0, database.characters.Count - 1);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnClickPrev);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnClickNext);

        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(OnClickPrev);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnClickNext);
    }

    public void OnClickPrev()
    {
        if (database == null || database.characters.Count == 0) return;

        int count = database.characters.Count;
        _currentIndex = (_currentIndex - 1 + count) % count;
        ApplySelection();
    }

    public void OnClickNext()
    {
        if (database == null || database.characters.Count == 0) return;

        int count = database.characters.Count;
        _currentIndex = (_currentIndex + 1) % count;
        ApplySelection();
    }

    private void ApplySelection()
    {
        RefreshDisplay();

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

        Debug.Log($"[CharSelect] Index={_currentIndex} | FoundSelector={_localSelector != null}");

        if (_localSelector != null)
        {
            _localSelector.SelectCharacter(_currentIndex);
        }
        else
        {
            PlayerPrefs.SetInt("SelectedCharacterIndex", _currentIndex);
        }
    }

    private void RefreshDisplay()
    {
        if (database == null || database.characters.Count == 0) return;

        CharacterVisualData data = database.GetCharacter(_currentIndex);
        if (characterNameText != null && data != null)
        {
            characterNameText.text = string.IsNullOrEmpty(data.characterName)
                ? $"Character {_currentIndex + 1}"
                : data.characterName;
        }

        if (indexCounterText != null)
        {
            indexCounterText.text = $"{_currentIndex + 1} / {database.characters.Count}";
        }
    }
}