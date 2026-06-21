using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class AvatarCropper
{

    public static UniTask<Texture2D> CropImageAsync(string filePath)
    {
        var tcs = new UniTaskCompletionSource<Texture2D>();

        // 1. Đọc file ảnh gốc từ đường dẫn thành Texture2D
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D originalTexture = new Texture2D(2, 2);
        originalTexture.LoadImage(fileData);

        // 2. Cấu hình Package (Tùy thuộc vào mỗi package sẽ có cấu hình riêng)
        // Ví dụ cấu hình ép buộc cắt theo hình TRÒN hoặc TỶ LỆ 1:1 giống PUBG:
        var settings = new ImageCropper.Settings()
        {

            
            ovalSelection = true,
            autoZoomEnabled = true,
            selectionMinAspectRatio = 1,
            selectionMaxAspectRatio = 1,            
            markTextureNonReadable = false
        };
        // 3. Gọi Giao diện của Package lên
        ImageCropper.Instance.Show(originalTexture, (result, originalTex, croppedTex) =>
        {
            if (result)
            {
                Texture2D finalResult = croppedTex as Texture2D;
                Debug.Log(finalResult.isReadable);
                tcs.TrySetResult(finalResult);
            }
            else
            {               
                tcs.TrySetResult(null);
            }
        }, settings);      

        return tcs.Task;
    }


}