using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchPanel : MonoBehaviour
{
    [SerializeField] TMP_InputField Name;
    [SerializeField] TMP_InputField Tag;
    [SerializeField] RectTransform content;
    [SerializeField] Button Search;
    [SerializeField] GameObject emptyResultText;

    private string _lastQueryKey;
    private bool _isSearching;
    private const string RESAULTSEARCH_KEY = "RESULTSEARCHUI";

    private void Awake()
    {
        Tag.onValidateInput += OnValidateTag;
        Search.onClick.AddListener(OnSearchClick);
        Name.onSubmit.AddListener(_ => OnSearchClick());
        Tag.onSubmit.AddListener(_ => OnSearchClick());
    }

    private async void OnSearchClick()
    {
        if (_isSearching) return;

        string cleanName = Name.text != null ? Name.text.Trim() : string.Empty;
        string cleanTag = Tag.text != null ? Tag.text.Trim().TrimStart('#') : string.Empty;

        if (string.IsNullOrEmpty(cleanName))
        {
            ClearResults();
            if (emptyResultText != null) emptyResultText.SetActive(false);
            return;
        }

        string queryKey = $"{cleanName}#{cleanTag}";
        //if (queryKey == _lastQueryKey) return;

        _isSearching = true;
        if (Search != null) Search.interactable = false;

        try
        {
            _lastQueryKey = queryKey;
            var snapshot = await NetworkDataManager.Instance.friendManager.Search(cleanName, cleanTag);

            ClearResults();

            int shownCount = 0;
            string emptyReason = "No player found.";

            if (snapshot != null && snapshot.Length > 0)
            {
                foreach (var child in snapshot)
                {
                    //if (child.Key == FirebaseManager.UserID || NetworkDataManager.Instance.friendManager.IsFriend(child.Key))
                    //    continue;

                    if (child.Key == FirebaseManager.UserID)
                    {
                        emptyReason = "Cannot add yourself.";
                        continue;
                    }

                    if (NetworkDataManager.Instance.friendManager.IsFriend(child.Key))
                    {
                        emptyReason = "Already in your friend list.";
                        continue;
                    }

                    string rawPresence = child.Child("Presence").GetRawJsonValue();
                    if (string.IsNullOrEmpty(rawPresence)) continue;

                    Presence presence = JsonConvert.DeserializeObject<Presence>(rawPresence);
                    if (NetworkDataManager.Instance.GetPresenceUser(child.Key) != null)
                        await NetworkDataManager.Instance.UpddatePresenceData(child.Key, presence);
                    else
                        await NetworkDataManager.Instance.LoadPresence(child.Key, presence);

                    //GameObject element = ObjectPoolManager.Ins.Get(RESAULTSEARCH_KEY, content);
                    //ResultSearchUI scripts = element.GetComponent<ResultSearchUI>();
                    //scripts.UpdateUI(child.Key);
                    //shownCount++;

                    GameObject element = ObjectPoolManager.Ins.Get(RESAULTSEARCH_KEY, content, false);
                    if (element != null && element.TryGetComponent<ResultSearchUI>(out var scripts))
                    {
                        scripts.UpdateUI(child.Key);
                        shownCount++;
                    }
                }
            }

            Debug.Log($"[SearchPanel] Query '{queryKey}' -> Raw: {snapshot?.Length ?? 0}, Shown: {shownCount}");

            if (emptyResultText != null)
            {
                emptyResultText.SetActive(shownCount == 0);
                if (shownCount == 0)
                {
                    var tmp = emptyResultText.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null) tmp.text = emptyReason;
                }
            }
        }
        finally
        {
            _isSearching = false;
            if (Search != null) Search.interactable = true;
        }
    }

    private void ClearResults()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject obj = content.GetChild(i).gameObject;
            //if (obj.activeSelf)
            //{
            //    obj.GetComponent<ResultSearchUI>()?.ResetUI();
            //    ObjectPoolManager.Ins.Release(RESAULTSEARCH_KEY, obj);
            //}
            if (obj.activeSelf && obj.TryGetComponent<ResultSearchUI>(out var resultItem))
            {
                resultItem.ResetUI();
                ObjectPoolManager.Ins.Release(RESAULTSEARCH_KEY, obj);
            }
        }
    }

    private char OnValidateTag(string text, int charIndex, char addedChar)
    {
        if (char.IsWhiteSpace(addedChar) || addedChar == '#') return '\0';
        return char.ToUpperInvariant(addedChar);
    }

    private void OnDestroy()
    {
        if (Tag != null) Tag.onValidateInput -= OnValidateTag;
        if (Search != null) Search.onClick.RemoveAllListeners();
        if (Name != null) Name.onSubmit.RemoveAllListeners();
        if (Tag != null) Tag.onSubmit.RemoveAllListeners();
    }

    private void OnDisable()
    {
        _lastQueryKey = null;
        ClearResults();
        if (emptyResultText != null) emptyResultText.SetActive(false);
    }
}