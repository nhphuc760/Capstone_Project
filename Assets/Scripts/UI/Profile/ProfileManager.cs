using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;
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
    Presence myPresence;
    // Token dùng để hủy bỏ khẩn cấp các tác vụ async đang chạy
    private CancellationTokenSource _cts;

    // Kiểm tra xem có đang trong tiến trình chỉnh sửa nào không
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
        avatar.sprite = NetworkDataManager.Instance.GetAvatarUser(userID);
        _name.text = myPresence.Name;
        Avatar.sprite = NetworkDataManager.Instance.GetAvatarUser(userID);
        Name.text = myPresence.Name;
        Tag.text = myPresence.Tag;
        LockNameInputField(true);
        LockTagInputField(true);
    }


    /// <summary>
    /// Hàm này được gọi từ UI khi bấm vào nút Profile
    /// </summary>
    public void ProfileClick()
    {
        if (!IsEditing || RoomDatabaseManager.Instance.CurrentRoom == null)
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
        if (IsEditing) return;
        panel.gameObject.SetActive(false);
    }

    



    public async void OnChangeAvatarButtonClicked()
    {
        if (IsEditing) return;

        // Khởi tạo Token Source mới cho tiến trình này
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        try
        {
            // 1. Chọn File
            UpdateState(ProfileEditState.ChoosingFile);
            var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };

            // Giả định FilePicker hỗ trợ truyền CancellationToken hoặc ta tự check sau khi chạy xong
            string filePath = await FilePicker.ChooseFileAsync("Select Avatar", "", extensions, false);
            token.ThrowIfCancellationRequested(); // Kiểm tra xem có lệnh hủy từ bên ngoài không

            if (string.IsNullOrEmpty(filePath) || !IsFileSizeValid(filePath))
            {
                CancelProcess();
                return;
            }

            // 2. Cắt ảnh
            UpdateState(ProfileEditState.CroppingImage);
            Texture2D croppedAvatar = await AvatarCropper.CropImageAsync(filePath); // Nếu AvatarCropper có nhận Token thì truyền vào
            token.ThrowIfCancellationRequested();

            if (croppedAvatar == null)
            {
                CancelProcess();
                return;
            }
            

            ImageCropper.Instance.Hide();

            // 3. Upload
            UpdateState(ProfileEditState.Uploading);
            Texture2D resize = croppedAvatar.ResizeTexture(256, 256);
            Sprite avt = resize.ToSprite();
            avatar.sprite = avt;
            Avatar.sprite = avt;
            Destroy(croppedAvatar); // Giải phóng RAM sớm
            byte[] uploadBytes = resize.EncodeToJPG(90);
            Debug.Log($"Upload Size: {uploadBytes.Length / 1024f:F2} KB");
            Destroy(resize);
            // Truyền token vào bộ Uploader (Nếu ImgbbUploader hỗ trợ Task)
            string url = await ImgbbUploader.UploadAvatarBytesAsync(uploadBytes);
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(url))
            {
                
                Debug.LogError("Lưu ảnh thất bại");
                CancelProcess();
                return;
            }

            // 4. Lưu Firestore
            Debug.Log("Tải ảnh thành công: " + url);
            await FirebaseManager.RealtimeDB.SetValue($"Users/{FirebaseManager.UserID}/Presence/AvatarUrl", url);

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
            // Cuối cùng luôn đưa trạng thái về Idle và dọn dẹp Token
            Debug.Log("Reset State finally");
            ResetState();
        }
    }

    /// <summary>
    /// HÀM QUAN TRỌNG: Gọi hàm này khi người chơi Chấp nhận lời mời Party / Vào Room đột ngột.
    /// Nó sẽ lập tức bẻ gãy mạch Async đang chạy để đóng tiến trình an toàn.
    /// </summary>
    public void ForceInterruptAndClose()
    {
        if (!IsEditing) return;

        Debug.Log("Phát hiện can thiệp hệ thống! Đang đóng tiến trình Profile...");

        // Kích hoạt hủy bỏ tác vụ async
        _cts?.Cancel();

        // Thu dọn các UI đang mở hiển thị cắt ảnh
        if (ImageCropper.Instance != null)
        {
            ImageCropper.Instance.Hide();
        }

        // Đưa trạng thái về Idle
        ResetState();
    }

    private void UpdateState(ProfileEditState newState)
    {
        currentState = newState;
        // Bạn có thể trigger thêm UI loading tương ứng với từng State tại đây
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
    }

    void DesubcribeEventUI()
    {
        Tag.onValidateInput -= OnValidateTag;
        EditName.onClick.RemoveAllListeners();
        EditTag.onClick.RemoveAllListeners();
        Name.onEndEdit.RemoveAllListeners();
        Tag.onEndEdit.RemoveAllListeners();
        EditAvatar.onClick.RemoveAllListeners();
    }
    private void OnDestroy()
    {
        DesubcribeEventUI();
    }

    #region NameTag Edit
    void LockNameInputField(bool value)
    {
        Name.readOnly = value;      
        Name.image.enabled = !value;       
    }
    void LockTagInputField(bool value)
    {
        Tag.readOnly = value;
        Tag.image.enabled = !value;
    }

    void OnEndEditName(string text)
    {
        // Popup xác nhận
        // Nếu xác nhận thì lưu tên text, nếu không Name.text = myPresence.Name;
        Debug.Log("EndEditName Called");
        UpdateState(ProfileEditState.Idle);
        LockNameInputField(true);
    }
    void OnEndEditTag(string text)
    {
        //Popup xác nhận ở đây
        // Nếu xác nhận thì lưu tag text, nếu không Tag.text = myPresence.Tag;
        Debug.Log("EndEditTag called");
        UpdateState(ProfileEditState.Idle);
        LockTagInputField(true);
    }

    char OnValidateTag(string text, int charIndex, char addedChar)
    {
        return char.ToUpperInvariant(addedChar);
    }
    public void EditTagClick()
    {
        if (IsEditing) return;
        LockTagInputField(false);
        UpdateState(ProfileEditState.ChangingTag);
        Tag.ActivateInputField();
    }
    public void EditNameClick()
    {
        if (IsEditing) return;
        LockNameInputField(false);
        UpdateState(ProfileEditState.ChangingName);
        Name.ActivateInputField();
    }
    #endregion
}