using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ProfileEditState
{
    Idle,
    ChoosingFile,
    CroppingImage,
    Uploading,
    ChangingName,
    ChangingTag
}

public class ProfileManager : MonoBehaviour
{
    [SerializeField] private ProfileEditState currentState = ProfileEditState.Idle;
    [Header("Popup")]
    [SerializeField] Image avatar;
    [SerializeField] TextMeshProUGUI _name;
    [Header("ProfileSettingPanel")]
    [SerializeField] RectTransform panel;
    [SerializeField] Image Avatar;
    [SerializeField] Button EditAvatar;
    [SerializeField] TMP_InputField Name;
    [SerializeField] TMP_InputField Tag;
    [SerializeField] Button EditName;
    [SerializeField] Button EditTag;
    [SerializeField] Button CancelButton;
    [SerializeField] Button SaveButton;

    Presence myPresence;
    private CancellationTokenSource _cts;

    // Giữ bản cache cục bộ theo đúng UserID để không bao giờ bị reset khi load lại LobbyScene
    private static string _cachedUserId;
    private static Sprite _cachedAvatarSprite;

    public bool IsEditing => currentState != ProfileEditState.Idle;

    private void Awake()
    {
        Initialize();
        SubcribeEventUI();
    }

    void Initialize()
    {
        string userID = FirebaseManager.UserID;
        myPresence = NetworkDataManager.Instance.GetMyPresence();

        Sprite currentAvatar = (!string.IsNullOrEmpty(userID) && userID == _cachedUserId && _cachedAvatarSprite != null)
            ? _cachedAvatarSprite
            : NetworkDataManager.Instance.GetAvatarUser(userID);

        if (currentAvatar != null)
        {
            avatar.sprite = currentAvatar;
            avatar.color = Color.white;
            Avatar.sprite = currentAvatar;
            Avatar.color = Color.white;
        }

        _name.text = myPresence.Name;
        Name.text = myPresence.Name;
        Tag.text = myPresence.Tag;
        LockNameInputField(true);
        LockTagInputField(true);
    }

    public void ProfileClick()
    {
        if (!IsEditing || (RoomDatabaseManager.Instance != null && RoomDatabaseManager.Instance.CurrentRoom == null))
        {
            panel.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Không thể thực hiện hành động lúc này, vui lòng thoát khỏi đội hoặc hoàn thành tiến trình hiện tại");
        }
    }

    public void PanelClick()
    {
        CancelClick();
    }

    public void CancelClick()
    {
        if (IsEditing)
        {
            ForceInterruptAndClose();
        }

        if (myPresence != null)
        {
            if (Name != null) Name.text = myPresence.Name;
            if (Tag != null) Tag.text = myPresence.Tag;
        }

        LockNameInputField(true);
        LockTagInputField(true);
        ResetState();

        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    public void SaveClick()
    {
        if (Name != null && !Name.readOnly)
            OnEndEditName(Name.text);

        if (Tag != null && !Tag.readOnly)
            OnEndEditTag(Tag.text);

        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    public async void OnChangeAvatarButtonClicked()
    {
        if (IsEditing) return;

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        try
        {
            // 1. Chọn File
            UpdateState(ProfileEditState.ChoosingFile);
            var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };

            string filePath = await FilePicker.ChooseFileAsync("Select Avatar", "", extensions, false);
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(filePath) || !IsFileSizeValid(filePath))
            {
                CancelProcess();
                return;
            }

            // 2. Cắt ảnh
            UpdateState(ProfileEditState.CroppingImage);
            Texture2D croppedAvatar = await AvatarCropper.CropImageAsync(filePath);
            token.ThrowIfCancellationRequested();

            if (croppedAvatar == null)
            {
                CancelProcess();
                return;
            }

            if (ImageCropper.Instance != null)
                ImageCropper.Instance.Hide();

            // 3. Resize & hiển thị ngay trên UI
            UpdateState(ProfileEditState.Uploading);
            Texture2D resize = croppedAvatar.ResizeTexture(256, 256);
            Sprite avt = resize.ToSprite();

            if (avatar != null)
            {
                avatar.sprite = avt;
                avatar.color = Color.white;
                avatar.enabled = true;
            }
            if (Avatar != null)
            {
                Avatar.sprite = avt;
                Avatar.color = Color.white;
                Avatar.enabled = true;
            }

            Destroy(croppedAvatar);

            byte[] uploadBytes = resize.EncodeToJPG(90);
            Debug.Log($"Upload Size: {uploadBytes.Length / 1024f:F2} KB");

            string url = await ImgbbUploader.UploadAvatarBytesAsync(uploadBytes);
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("Lưu ảnh thất bại");
                CancelProcess();
                return;
            }

            // 4. Lưu RealtimeDB & ghi đè cache của NetworkDataManager
            string userID = FirebaseManager.UserID;
            Debug.Log("Tải ảnh thành công: " + url);

            if (myPresence != null)
                myPresence.AvatarUrl = url;

            _cachedUserId = userID;
            _cachedAvatarSprite = avt;

            if (NetworkDataManager.Instance != null)
            {
                NetworkDataManager.Instance.UpdateAvatarCache(userID, avt, url);
            }

            await FirebaseManager.RealtimeDB.SetValue($"Users/{userID}/Presence/AvatarUrl", url);

            Debug.Log("Cập nhật Avatar hoàn tất thành công!");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Tiến trình chỉnh sửa Profile đã bị đóng an toàn do có sự kiện khẩn cấp.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi không xác định: {ex.Message}");
        }
        finally
        {
            Debug.Log("Reset State finally");
            ResetState();
        }
    }

    public void ForceInterruptAndClose()
    {
        if (!IsEditing) return;

        Debug.Log("Phát hiện can thiệp hệ thống! Đang đóng tiến trình Profile...");

        _cts?.Cancel();

        if (ImageCropper.Instance != null)
        {
            ImageCropper.Instance.Hide();
        }

        if (Name != null)
        {
            if (myPresence != null) Name.text = myPresence.Name;
            LockNameInputField(true);
        }

        if (Tag != null)
        {
            if (myPresence != null) Tag.text = myPresence.Tag;
            LockTagInputField(true);
        }

        ResetState();
    }

    private void UpdateState(ProfileEditState newState)
    {
        currentState = newState;
    }

    private void CancelProcess()
    {
        ResetState();
    }

    private void ResetState()
    {
        currentState = ProfileEditState.Idle;
        if (_cts != null)
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private bool IsFileSizeValid(string filePath, long maxBytes = 5 * 1024 * 1024)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists && fileInfo.Length <= maxBytes) return true;

        Debug.LogError("File không tồn tại hoặc quá dung lượng.");
        return false;
    }

    void SubcribeEventUI()
    {
        Tag.onValidateInput += OnValidateTag;
        EditName.onClick.AddListener(EditNameClick);
        EditTag.onClick.AddListener(EditTagClick);
        Name.onEndEdit.AddListener(OnEndEditName);
        Tag.onEndEdit.AddListener(OnEndEditTag);
        EditAvatar.onClick.AddListener(OnChangeAvatarButtonClicked);

        if (CancelButton != null)
            CancelButton.onClick.AddListener(CancelClick);
        if (SaveButton != null)
            SaveButton.onClick.AddListener(SaveClick);
    }

    void DesubcribeEventUI()
    {
        Tag.onValidateInput -= OnValidateTag;
        EditName.onClick.RemoveAllListeners();
        EditTag.onClick.RemoveAllListeners();
        Name.onEndEdit.RemoveAllListeners();
        Tag.onEndEdit.RemoveAllListeners();
        EditAvatar.onClick.RemoveAllListeners();

        if (CancelButton != null)
            CancelButton.onClick.RemoveAllListeners();
        if (SaveButton != null)
            SaveButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        DesubcribeEventUI();
    }

    #region NameTag Edit
    void LockNameInputField(bool value)
    {
        Name.readOnly = value;
        Name.interactable = !value;
        if (Name.image != null)
            Name.image.enabled = !value;
    }

    void LockTagInputField(bool value)
    {
        Tag.readOnly = value;
        Tag.interactable = !value;
        if (Tag.image != null)
            Tag.image.enabled = !value;
    }

    void OnEndEditName(string text)
    {
        if (currentState != ProfileEditState.ChangingName) return;

        bool wasCanceled = Name.wasCanceled;
        LockNameInputField(true);
        UpdateState(ProfileEditState.Idle);

        string trimmedName = text != null ? text.Trim() : "";
        if (wasCanceled || string.IsNullOrEmpty(trimmedName) || (myPresence != null && trimmedName == myPresence.Name))
        {
            if (myPresence != null) Name.text = myPresence.Name;
            return;
        }

        SaveNameAsync(trimmedName).Forget();
    }

    void OnEndEditTag(string text)
    {
        if (currentState != ProfileEditState.ChangingTag) return;

        bool wasCanceled = Tag.wasCanceled;
        LockTagInputField(true);
        UpdateState(ProfileEditState.Idle);

        string trimmedTag = text != null ? text.Trim().ToUpperInvariant() : "";
        if (wasCanceled || string.IsNullOrEmpty(trimmedTag) || (myPresence != null && trimmedTag == myPresence.Tag))
        {
            if (myPresence != null) Tag.text = myPresence.Tag;
            return;
        }

        SaveTagAsync(trimmedTag).Forget();
    }

    private async UniTaskVoid SaveNameAsync(string newName)
    {
        string oldName = myPresence != null ? myPresence.Name : _name.text;
        Name.text = newName;
        if (_name != null) _name.text = newName;
        if (myPresence != null) myPresence.Name = newName;

        try
        {
            await FirebaseManager.RealtimeDB.SetValue($"Users/{FirebaseManager.UserID}/Presence/Name", newName);
            Debug.Log("Cập nhật Name thành công: " + newName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi lưu Name: {ex.Message}");
            Name.text = oldName;
            if (_name != null) _name.text = oldName;
            if (myPresence != null) myPresence.Name = oldName;
        }
    }

    private async UniTaskVoid SaveTagAsync(string newTag)
    {
        string oldTag = myPresence != null ? myPresence.Tag : "";
        Tag.text = newTag;
        if (myPresence != null) myPresence.Tag = newTag;

        try
        {
            await FirebaseManager.RealtimeDB.SetValue($"Users/{FirebaseManager.UserID}/Presence/Tag", newTag);
            Debug.Log("Cập nhật Tag thành công: " + newTag);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi lưu Tag: {ex.Message}");
            Tag.text = oldTag;
            if (myPresence != null) myPresence.Tag = oldTag;
        }
    }

    char OnValidateTag(string text, int charIndex, char addedChar)
    {
        if (char.IsWhiteSpace(addedChar)) return '\0';
        return char.ToUpperInvariant(addedChar);
    }

    public void EditTagClick()
    {
        if (currentState == ProfileEditState.ChangingTag)
        {
            OnEndEditTag(Tag.text);
            return;
        }
        if (IsEditing) return;

        LockTagInputField(false);
        UpdateState(ProfileEditState.ChangingTag);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Tag.gameObject);
        Tag.ActivateInputField();
        Tag.MoveTextEnd(false);
    }

    public void EditNameClick()
    {
        if (currentState == ProfileEditState.ChangingName)
        {
            OnEndEditName(Name.text);
            return;
        }
        if (IsEditing) return;

        LockNameInputField(false);
        UpdateState(ProfileEditState.ChangingName);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Name.gameObject);
        Name.ActivateInputField();
        Name.MoveTextEnd(false);
    }
    #endregion
}