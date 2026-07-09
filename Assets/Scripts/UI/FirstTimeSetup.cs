using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class FirstTimeSetup : MonoBehaviour
{
    [SerializeField] TMP_InputField Name;
    [SerializeField] TMP_InputField Tag;
    [SerializeField] Button Continue;
    [SerializeField] RectTransform LogError;
    [SerializeField] TextMeshProUGUI LogMessage;

    private void Awake()
    {
        Continue.onClick.AddListener(ContinueClick);
        Tag.onValidateInput += OnValidateTag;
    }


    char OnValidateTag(string text, int charIndex, char addedChar)
    {
        return char.ToUpperInvariant(addedChar);
    }

    async void ContinueClick()
    {
        if (string.IsNullOrEmpty(Name.text))
        {
            ShowLog("Tên không được để trống");
            return;
        }
        if (string.IsNullOrEmpty(Tag.text))
        {
            ShowLog("Tag không được để trống"); 
            return;
        }
        Continue.interactable = false;
        string name = Name.text.Trim();
        string tag = Tag.text.Trim();
        if (tag.Length > 4)
        {
            ShowLog("Lỗi dữ liệu tag không hợp lệ");
            return;
        }
        var valid = await CheckValidNameTag(name, tag);
        if (valid)
        {
            Debug.Log("Name tag valid");
            Presence myPresence = new Presence
            {
                Name = name,
                Tag = tag,
                Status = OnlineStatus.Online,
                AvatarUrl = null
            };
            await NetworkDataManager.Instance.UpdateMyPresence(myPresence);
            await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence/Status").OnDisconnect().SetValue((int)OnlineStatus.Offline);
            await SceneController.Instance.NewTransitionPlan()
                                   .Load(new ParameterScene { Name = SceneDatabase.LOBBY }, true)
                                   .UnLoad(new ParameterScene { Name = SceneDatabase.MAINMENU })
                                   .WithFadeIn()
                                   .WithFadeOut()
                                   .Perform();
        }
        else
        {
            ShowLog("Name và Tag đã có người sử dụng, hãy thay đổi một trong hai để tiếp tục");
            Continue.interactable = true;
        }
    }  

    void ShowLog(string message)
    {
        if (LogError == null || LogMessage == null)
        {
            Debug.Log("Null LogError và LogMessage");
            return;
        }
        LogMessage.text = message;
        LogError.gameObject.SetActive(true);
    }

    async UniTask<bool> CheckValidNameTag(string name, string tag)
    {
        DatabaseReference @ref = FirebaseManager.RealtimeDB.reference;
        var query = await @ref.Child("Users").OrderByChild("Presence/Name").EqualTo(name).GetValueAsync();
        if (query.Exists)
        {
            foreach (var i in query.Children)
            {
                var tagI = i.Child("Presence/Tag");
                if (tagI.Exists)
                {
                    string tagCmp = tagI.Value.ToString();
                    Debug.Log(tag);
                    if (tag == tagCmp)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

}
