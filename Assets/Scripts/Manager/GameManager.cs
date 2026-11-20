using System;
using Fusion;
using Gamelogic.Extensions;
using UnityEngine;
using Util.Core;

public enum GameState:byte
{
    None,
    WaitingForPlayers,
    Countdown,
    Playing,
    MatchEnd
}

public class GameManager : NetworkBehaviour, IPlayerJoined
{
    public static GameManager Instance { get; private set; }
    [Header("References")]
    [SerializeField] NetworkObject shopRef;
    [SerializeField] LayerMask obstacleMap;
    [SerializeField] MapManager mapManager;
    [SerializeField] ResourceManager resourceManager;
    [SerializeField] NavMeshBakeManager navMeshBakeManager;
    [SerializeField] DayNightManager dayNightManager;
    [SerializeField] NetworkPrefabRef playerPrefab;
    [SerializeField] int MaxPlayers = 1;
    [Networked]
    public GameState CurrentState { get; private set; }

    [Networked]
    public TickTimer MatchTimer { get; private set; }



    int PlayerCount = 1;

    public event Action<GameState> OnGameStateChanged;


    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (HasStateAuthority)
        {
            InitialData();
        }
    }


    void InitialData()
    {
        Debug.Log("[MapManager] Create map");
        mapManager.GenerateMap();
        // Báo cho ResourceManager khởi tạo resource nodes           
        resourceManager.InitializeResources();
        //SpawnShop
        var position = mapManager.FindValidSpawnPositionInGreen(shopRef.GetComponent<BoxCollider>().bounds, obstacleMap);
        if(position != null)
        {
            Vector3 pos = position.Value;
            pos.y = 0;
            shopRef.GetBehaviour<NetworkTransform>().Teleport(pos);
        }

        NavMeshBakeManager.Ins.BakeAll();
        Runner.Spawn(playerPrefab, shopRef.transform.Find("SpawnPoint").position.RandomAround(1f), Quaternion.identity, Runner.LocalPlayer, (runner, obj) =>
        {
            obj.name = Object.InputAuthority.ToString();
            runner.SetPlayerObject(Runner.LocalPlayer, obj);
        });
        SetState(GameState.WaitingForPlayers);
    }



    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        switch (CurrentState)
        {
            case GameState.WaitingForPlayers:
                HandleWaitingPlayers();
                break;

            case GameState.Countdown:
                HandleCountdown();
                break;

            case GameState.Playing:
                HandlePlaying();
                break;           

            case GameState.MatchEnd:
                HandleMatchEnd();
                break;
        }
    }

    public void SetState(GameState newState)
    {
        if (!Object.HasStateAuthority)
            return;

        if (CurrentState == newState)
            return;

        CurrentState = newState;

        Debug.Log($"Game State -> {newState}");

        RPC_GameStateChanged(newState);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameStateChanged(GameState state)
    {
        OnGameStateChanged?.Invoke(state);
    }

    private void HandleWaitingPlayers()
    {
        if (PlayerCount >= MaxPlayers)
        {
            StartCountdown(5f);
        }
    }
    private void StartCountdown(float duration)
    {
        MatchTimer = TickTimer.CreateFromSeconds(Runner, duration);

        SetState(GameState.Countdown);
    }

    private void HandleCountdown()
    {
        if (MatchTimer.Expired(Runner))
        {
            StartMatch();
        }
    }
    private void StartMatch()
    {
        Debug.Log("Match Start");

        MatchTimer = TickTimer.CreateFromSeconds(Runner, 300);

        SetState(GameState.Playing);
    }

    private void HandlePlaying()
    {
        if (MatchTimer.Expired(Runner))
        {
            EndMatch();
        }
    }
    private void EndMatch()
    {
        Debug.Log("Match End");

        SetState(GameState.MatchEnd);
    }
    
    private void HandleMatchEnd()
    {

    }

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority && Runner.LocalPlayer != player)
        {
            Runner.Spawn(playerPrefab,shopRef.transform.Find("SpawnPoint").position.RandomAround(1f), Quaternion.identity, player, (runner, obj) =>
            {
                obj.name = player.ToString();
                runner.SetPlayerObject(player, obj);
            });
            PlayerCount++;          
            
        }
    }

    public Vector3 SpawnPoint()
    {
        return shopRef.transform.Find("SpawnPoint").position.RandomAround(1f);
    }
}
