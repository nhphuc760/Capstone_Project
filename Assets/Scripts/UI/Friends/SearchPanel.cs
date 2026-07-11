using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SearchPanel : MonoBehaviour
{
    [SerializeField] TMP_InputField Name;
    [SerializeField] TMP_InputField Tag;
    [SerializeField] RectTransform content;
    [SerializeField] Button Search;
    string _name;
    string _tag;
    const string RESAULTSEARCH_KEY = "RESULTSEARCHUI";
    private void Awake()
    {
        Tag.onValidateInput += OnValidateTag;
        Search.onClick.AddListener(OnSearchClick);
    }

    async void OnSearchClick()
    {
        if (Name.text.Trim() == _name && Tag.text.Trim() == _tag)
        {
            return;
        }
        _name = Name.text.Trim();
        _tag = Tag.text.Trim();
        var snapshot = await NetworkDataManager.Instance.friendManager.Search(_name, _tag);
        if (snapshot != null)
        {
            for (int i = content.childCount - 1; i>= 0; i--)
            {
                GameObject obj = content.GetChild(i).gameObject;
                if (obj.activeSelf)
                {
                    ObjectPoolManager.Ins.Release(RESAULTSEARCH_KEY, obj);
                }
            }
            foreach (var child in snapshot)
            {
                if (NetworkDataManager.Instance.friendManager.IsFriend(child.Key))
                {
                    continue;
                }
                Presence presence = JsonConvert.DeserializeObject<Presence>(child.Child("Presence").GetRawJsonValue());
                await NetworkDataManager.Instance.UpdatePresenceData(child.Key, presence); 
                GameObject element = ObjectPoolManager.Ins.Get(RESAULTSEARCH_KEY, content);
                ResultSearchUI scripts = element.GetComponent<ResultSearchUI>();
                scripts.UpdateUI(child.Key);
            }
        }
        else
        {
            Debug.Log("Không tìm thấy người chơi nào");
        }
    }

    char OnValidateTag(string text, int charIndex, char addedChar)
    {
        return char.ToUpperInvariant(addedChar);
    }
    private void OnDestroy()
    {
        Tag.onValidateInput -= OnValidateTag;
        Search.onClick.RemoveAllListeners();
    }
}
