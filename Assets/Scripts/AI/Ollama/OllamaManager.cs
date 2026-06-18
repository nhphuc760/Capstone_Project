using System;
using System.Collections;
using System.Diagnostics;
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
        Stopwatch stopwatch =
            new Stopwatch();

        stopwatch.Start();

        UnityEngine.Debug.Log(
            "========== OLLAMA REQUEST ==========");

        OllamaRequest request =
            new OllamaRequest
            {
                model = "qwen3:4b",
                prompt = prompt,
                stream = false
            };

        string json =
            JsonUtility.ToJson(request);

        UnityEngine.Debug.Log(
            "JSON SENT:");

        UnityEngine.Debug.Log(
            json);

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

        UnityEngine.Debug.Log(
            "Sending Request...");

        yield return webRequest.SendWebRequest();

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            "Request Finished");

        UnityEngine.Debug.Log(
            $"AI Response Time: {stopwatch.ElapsedMilliseconds} ms");

        UnityEngine.Debug.Log(
            $"Result: {webRequest.result}");

        UnityEngine.Debug.Log(
            $"Response Code: {webRequest.responseCode}");

        if (webRequest.downloadHandler != null)
        {
            UnityEngine.Debug.Log(
                "RAW RESPONSE:");

            UnityEngine.Debug.Log(
                webRequest.downloadHandler.text);
        }

        if (webRequest.result ==
            UnityWebRequest.Result.Success)
        {
            try
            {
                OllamaResponse response =
                    JsonUtility.FromJson<OllamaResponse>(
                        webRequest.downloadHandler.text);

                if (response == null)
                {
                    UnityEngine.Debug.LogError(
                        "Response NULL");

                    yield break;
                }

                UnityEngine.Debug.Log(
                    "PARSED RESPONSE:");

                UnityEngine.Debug.Log(
                    response.response);

                callback?.Invoke(
                    response.response);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(
                    "JSON PARSE ERROR");

                UnityEngine.Debug.LogError(
                    e);
            }
        }
        else
        {
            UnityEngine.Debug.LogError(
                "REQUEST FAILED");

            UnityEngine.Debug.LogError(
                webRequest.error);
        }
    }
}