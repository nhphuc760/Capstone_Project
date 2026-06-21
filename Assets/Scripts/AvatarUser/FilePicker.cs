using UnityEngine;
using SFB;
using Cysharp.Threading.Tasks;
public static class FilePicker
{
    /// <summary>
    /// return string path
    /// </summary>
    /// <param name="title"></param>
    /// <param name="directory"></param>
    /// <param name="extension"></param>
    /// <param name="multiselect"></param>
    /// <returns></returns>
    public static UniTask<string> ChooseFileAsync(string title, string directory, ExtensionFilter[] extension, bool multiselect)
    {
        var uniTask = new UniTaskCompletionSource<string>();
        // Gọi hàm Async: Hàm này sẽ chạy ngầm và KHÔNG làm treo game
        StandaloneFileBrowser.OpenFilePanelAsync(title, directory, extension, multiselect, (string[] paths) =>
        {
            // Đoạn code bên trong này sẽ CHỈ CHẠY sau khi người chơi đã chọn xong ảnh hoặc tắt cửa sổ
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                uniTask.TrySetResult(paths[0]);
            }
            else
            {
                uniTask.TrySetResult(null);
            }
        });
        return uniTask.Task;
    }

}
