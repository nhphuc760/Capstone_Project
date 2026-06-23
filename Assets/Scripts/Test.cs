using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    bool isChanging = false;
    private void Update()
    {
        //if (Mouse.current.leftButton.wasPressedThisFrame && !isChanging)
        //{
        //    OnChangeAvatarButtonClicked();
        //}      
    }

    public async void OnChangeAvatarButtonClicked()
    {
        isChanging = true;
        var extensions = new[] {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
        };
        string filePath = await FilePicker.ChooseFileAsync("Select Avatar", "", extensions, false);
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.Log("Người chơi đã hủy, dừng tiến trình.");
            isChanging = false;
            return;
        }
        if (!IsFileSizeValid(filePath))
        {
            Debug.Log("Dung lượng file vượt quá mức quy định");
            isChanging = false;
            return;
        }



        Texture2D croppedAvatar = await AvatarCropper.CropImageAsync(filePath);


        if (croppedAvatar == null)
        {
            Debug.Log("Người chơi đã hủy cắt ảnh.");
            isChanging = false;
            return;
        }

        ImageCropper.Instance.Hide();
        // 4. Chuyển ảnh đã cắt thành byte dữ liệu để chuẩn bị upload
        Debug.Log(croppedAvatar.isReadable);
        byte[] uploadBytes = croppedAvatar.EncodeToPNG();

        // 5. Chờ hành động upload lên Server
        Debug.Log("Đang upload ảnh lên server...");
        string url = await ImgbbUploader.UploadAvatarBytesAsync(uploadBytes);

        if (string.IsNullOrEmpty(url))
        {
            Debug.Log("Lưu ảnh thất bại");
        }
        else
        {
            Debug.Log("Tải ảnh thành công: " + url);
            var data = new Dictionary<string, object>
            {
                { "avatarlink", url}
            };

            await FirebaseManager.FireStore.SetValue($"Users/{FirebaseManager.UserID}/", data);
            //Update avatar
            //Hàm SetUrl image lên firebase
        }
        Destroy(croppedAvatar);
        isChanging = false;
    }
   
    private bool IsFileSizeValid(string filePath, long maxBytes = 5 * 1024 * 1024) // 5MB mặc định
    {
        FileInfo fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists)
        {
            // Kiểm tra dung lượng file (tính bằng byte)
            if (fileInfo.Length > maxBytes)
            {
                Debug.LogError($"File quá lớn! Giới hạn: {maxBytes / 1024 / 1024}MB. File của bạn: {fileInfo.Length / 1024 / 1024}MB");
                return false;
            }
            return true;
        }
        return false;
    }
}
