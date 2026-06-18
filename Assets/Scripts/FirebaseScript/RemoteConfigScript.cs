using UnityEngine;
using Firebase.RemoteConfig;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;

[Serializable]
public class ConfigData
{
    public string roomID;
    public string playerID;
    public string playerName;
    public int playerLevel;
}

public class RemoteConfigScript : MonoBehaviour
{
    public ConfigData allConfigData;

    private void Awake()
    {
        CheckRemoteConfigValue();
        print("json: " + JsonUtility.ToJson(allConfigData));
    }

    public Task CheckRemoteConfigValue()
    {
        Debug.Log("fetching remote config value...");
        Task fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    public void FetchComplete(Task fetchTask)
    {
        //Check for errors in fetching the remote config value.
        if (!fetchTask.IsCompleted)
        {
            Debug.LogError("remote config value fetch did not complete.");
            return;
        }
        
        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info = remoteConfig.Info;
        if (info.LastFetchStatus != LastFetchStatus.Success)
        {
            Debug.LogError("remote config value fetch failed: " + info.LastFetchStatus);
            return;
        }
        
        //Fetch succeeded, now activate the fetched values.
        remoteConfig.ActivateAsync().ContinueWithOnMainThread(activateTask =>
        {
            string configData = remoteConfig.GetValue("TestingGame").StringValue;
            Debug.Log("Testing value: " + configData);

            allConfigData = JsonUtility.FromJson<ConfigData>(configData);
            Debug.Log("Parsed config json: " + JsonUtility.ToJson(allConfigData));

            // print("Total values: " + remoteConfig.AllValues.Count);

            // foreach (var item in remoteConfig.AllValues)
            // {
            //     print ("key: " + item.Key);
            //     print ("value: " + item.Value.StringValue);
            // }
        });
    }
}
