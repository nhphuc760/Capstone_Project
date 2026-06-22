using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class ImgbbUploader
{
    const string API_KEY =
        "a89b72820f3a3d20ea267387b071cf1a";

    /// <summary>
    /// return url image from imgBB to save anywhere(firebase)
    /// </summary>
    /// <param name="imageBytes"></param>
    /// <returns></returns>
    public static async UniTask<string> UploadAvatarBytesAsync(byte[] imageBytes)
    {
        string base64 =
            Convert.ToBase64String(imageBytes);

        WWWForm form = new WWWForm();

        form.AddField("key", API_KEY);
        form.AddField("image", base64);

        using UnityWebRequest request =
            UnityWebRequest.Post(
                "https://api.imgbb.com/1/upload",
                form
            );
        await request.SendWebRequest();
        if (request.result ==
         UnityWebRequest.Result.Success)
        {
            ImgBBRespone respone = JsonUtility.FromJson<ImgBBRespone>(request.downloadHandler.text);
            if(respone.success)
            {
                Debug.Log("Upload success");
                return respone.data.url;
            }
            else
            {
                return "";
            }                          
        }
        else
        {
            return "";
        }
    }

    public static async UniTask<Sprite> GetAvatar(string url)
    {
        UnityWebRequest request =
       UnityWebRequestTexture.GetTexture(url);

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Lỗi Load avatar: " + request.error);
            return null;
        }
        Texture2D texture =
            DownloadHandlerTexture
            .GetContent(request);

        Sprite sprite =
            Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height
                ),
                new Vector2(0.5f, 0.5f)
            );
        return sprite;
    }
}

