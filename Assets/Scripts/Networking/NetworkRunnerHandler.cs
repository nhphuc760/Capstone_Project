using UnityEngine;
using Fusion;
using Unity.VisualScripting;
using TriInspector;
using Cysharp.Threading.Tasks;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    [Required]
    NetworkRunner NetworkRunnerPrefab;
    [HideInInspector]
    public NetworkRunner _runner;
    public static NetworkRunnerHandler Ins { get; private set; }
    public bool InMatch { get; private set; } = false;
    public bool InParty { get; private set; } = false;
    private void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Destroy(gameObject);
            return;
        }
        Ins = this;
    }


    public async UniTask<StartGameResult> StartSession( string sessionName, int playerCount, Scene? sceneStart, byte[] connectionToken = default,  Dictionary<string, SessionProperty> sessionProperties = null, System.Action<NetworkRunner> onGameStarted = null, System.Action<NetworkRunner> hostmigrationResume = null, string customLobbyName = "Standard" )
    {
        await InitialRunner();
        return await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            Address = NetAddress.Any(),
            CustomLobbyName = customLobbyName,            
            SessionName = sessionName,
            SessionProperties = sessionProperties,
            SceneManager = GetSceneManager(),
            Scene = sceneStart != null ? SceneRef.FromIndex(sceneStart.Value.buildIndex) : null,
            ConnectionToken = connectionToken,
            PlayerCount = playerCount,
            OnGameStarted = onGameStarted == null ? OnGameStarted : onGameStarted,
            HostMigrationResume = hostmigrationResume,            
        });
    }
    
    public void JoinLobby(SessionLobby sessionLobby, string lobbyID = "OurLobbyID")
    {        
        _runner.JoinSessionLobby(sessionLobby, lobbyID);       
    }

    public async UniTask<StartGameResult> JoinSession(string sessionName, byte[] connectionToken = default, System.Action<NetworkRunner> onGameStarted = null)
    {
        await InitialRunner();
        return await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            ConnectionToken = connectionToken,
            OnGameStarted = onGameStarted == null ? OnGameStarted : onGameStarted
        });
    }    

    void OnGameStarted(NetworkRunner runner)
    {
        Debug.Log("OnGameStarted called in StartGameArgs");
        var foundObjects = FindObjectsByType<NetworkObject>();
        List<NetworkObject> sceneNetworkObjects = new List<NetworkObject>();
       
        foreach (var obj in foundObjects)
        {
            if (!obj.Id.IsValid && !obj.IsValid)
            {
                sceneNetworkObjects.Add(obj);
            }
        }
        Debug.Log($"NetworkObject in scene {SceneManager.GetActiveScene().name}: " + sceneNetworkObjects.Count);
        if (sceneNetworkObjects.Count > 0)
        {
            runner.RegisterSceneObjects(SceneRef.FromIndex(SceneManager.GetSceneByName(SceneDatabase.LOBBY).buildIndex), sceneNetworkObjects.ToArray());
        }
    }

    async UniTask InitialRunner()
    {
        if (_runner != null && _runner.IsRunning)
        {
            await _runner.Shutdown();         
            _runner = null;
        }
        if (_runner == null)
        {
            _runner = Instantiate(NetworkRunnerPrefab);
            InitialRunnerCallbacks();
        }
    } 
    void InitialRunnerCallbacks()
    {
        if (_runner == null) return;
        if (!_runner.TryGetComponent<INetworkRunnerCallbacks>(out var callbacks))
        {
            _runner.AddComponent<BasicSpawner>();
        }
    }   

    INetworkSceneManager GetSceneManager()
    {
        var sceneManager = _runner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
        {
            sceneManager = _runner.AddComponent<NetworkSceneManagerDefault>();
        }
        return sceneManager;
    }


    


}
