using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class OllamaRequest
{
    public string model;
    public string prompt;
    public bool stream;
}

[Serializable]
public class OllamaResponse
{
    public string response;
}

public class OllamaManager : MonoBehaviour
{
    private const string URL =
        "http://localhost:11434/api/generate";

    public void GenerateDialogue(
        string prompt,
        Action<string> callback)
    {
        StartCoroutine(
            GenerateRoutine(
                prompt,
                callback));
    }

    private IEnumerator GenerateRoutine(
        string prompt,
        Action<string> callback)
    {
        Debug.Log("========== OLLAMA REQUEST ==========");

        OllamaRequest request =
            new OllamaRequest
            {
                model = "qwen3:4b",
                prompt = prompt,
                stream = false
            };

        string json =
            JsonUtility.ToJson(request);

        Debug.Log("JSON SENT:");
        Debug.Log(json);

        UnityWebRequest webRequest =
            new UnityWebRequest(
                URL,
                "POST");

        byte[] body =
            Encoding.UTF8.GetBytes(json);

        webRequest.uploadHandler =
            new UploadHandlerRaw(body);

        webRequest.downloadHandler =
            new DownloadHandlerBuffer();

        webRequest.SetRequestHeader(
            "Content-Type",
            "application/json");

        Debug.Log("Sending Request...");

        yield return webRequest.SendWebRequest();

        Debug.Log("Request Finished");

        Debug.Log("Result:");
        Debug.Log(webRequest.result);

        Debug.Log("Response Code:");
        Debug.Log(webRequest.responseCode);

        if (webRequest.downloadHandler != null)
        {
            Debug.Log("RAW RESPONSE:");
            Debug.Log(webRequest.downloadHandler.text);
        }

        if (webRequest.result ==
            UnityWebRequest.Result.Success)
        {
            try
            {
                OllamaResponse response =
                    JsonUtility.FromJson<OllamaResponse>(
                        webRequest.downloadHandler.text);

                Debug.Log("PARSED RESPONSE:");
                Debug.Log(response.response);

                callback?.Invoke(
                    response.response);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "JSON PARSE ERROR");

                Debug.LogError(e);
            }
        }
        else
        {
            Debug.LogError(
                "REQUEST FAILED");

            Debug.LogError(
                webRequest.error);
        }
    }
}