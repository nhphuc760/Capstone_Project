using UnityEngine;
using Fusion;
using Unity.VisualScripting;
using TriInspector;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion.Sockets;
public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    [Required]
    NetworkRunner NetworkRunnerPrefab;
    NetworkRunner _runner;
    public static NetworkRunnerHandler Ins { get; private set; }

    private void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Destroy(gameObject);
            return;
        }
        Ins = this;
    }


    public async UniTask<StartGameResult> StartGame()
    {
        return await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            Address = NetAddress.Any(),
            Scene = null,
            SessionName = "",
            SceneManager = null,
            SessionProperties = default,
            PlayerCount = default,
            CustomLobbyName = default,
            OnGameStarted = default,
            HostMigrationToken = default,
            HostMigrationResume = default,
            ConnectionToken = default,
        });
    }
    
    public void JoinLobby(SessionLobby sessionLobby, string lobbyID = "OurLobbyID")
    {
        _runner.JoinSessionLobby(sessionLobby, lobbyID);
    }

    void InstantiateNetworkRunner()
    {
        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }
        _runner = Instantiate(NetworkRunnerPrefab);
        var scene = _runner.GetComponent<INetworkSceneManager>();
        if (scene == null)
        {
            scene = _runner.AddComponent<NetworkSceneManagerDefault>();
        }
    }





}
