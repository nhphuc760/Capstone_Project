using System;
using System.Linq;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using Util.Core;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public enum GameState : byte
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
    [SerializeField] GameObject gameOverUI;
    [SerializeField] NetworkObject shopRef;
    [SerializeField] LayerMask obstacleMap;
    [SerializeField] MapManager mapManager;
    [SerializeField] ResourceManager resourceManager;
    [SerializeField] NavMeshBakeManager navMeshBakeManager;
    [SerializeField] NetworkTimeManager dayNightManager;
    [SerializeField] NetworkPrefabRef playerPrefab;
    [SerializeField] float waitingTimer = 5f;
    [SerializeField] int MaxPlayers = 1;

    [Header("Elimination / End Game UI")]
    [SerializeField] private GameObject eliminatedUI;       // UI "Bạn đã bị loại"
    [SerializeField] private GameObject winUI;              // UI "Bạn thắng"
    [SerializeField] private CameraSwitcher cameraSwitcher; // Kéo CameraSwitcher vào đây

    [Header("Death Camera")]
    [SerializeField] private float monsterZoomDuration = 3f;

    [Networked]
    public GameState CurrentState { get; private set; }

    Dictionary<PlayerRef, Camera> camPlayers = new();
    public Dictionary<PlayerRef, Camera> Cameras => camPlayers;

    [Networked]
    public TickTimer MatchTimer { get; private set; }
    [Networked]
    public TickTimer CountDownTimer { get; private set; }

    int PlayerCount = 1;
    private readonly HashSet<PlayerRef> _spawnedPlayers = new();
    private readonly List<Vector3> _usedSpawnPositions = new();
    public event Action<GameState> OnGameStateChanged;

    // Camera đã detach để spectator (không bị hủy cùng player)
    private Camera spectatorMainCamera;

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
        resourceManager.InitializeResources();

        var position = mapManager.FindValidSpawnPositionInGreen(shopRef.GetComponent<Collider>().bounds, obstacleMap);
        if (position != null)
        {
            Vector3 pos = position.Value;
            pos.y = 0;
            shopRef.GetBehaviour<NetworkTransform>().Teleport(pos);
        }

        NavMeshBakeManager.Ins.BakeAll();
        //Runner.Spawn(playerPrefab, shopRef.transform.Find("SpawnPoint").position.RandomAround(1f), Quaternion.identity, Runner.LocalPlayer, (runner, obj) =>
        //{
        //    obj.name = Object.InputAuthority.ToString();
        //    runner.SetPlayerObject(Runner.LocalPlayer, obj);
        //});
        _spawnedPlayers.Clear();
        _usedSpawnPositions.Clear();

        var activePlayers = Runner.ActivePlayers.ToList();
        MaxPlayers = Mathf.Max(1, activePlayers.Count);
        PlayerCount = 0;

        foreach (var player in activePlayers)
        {
            SpawnPlayerAtRandomMapPosition(player);
        }
        SetState(GameState.WaitingForPlayers);
    }
    private void SpawnPlayerAtRandomMapPosition(PlayerRef player)
    {
        if (!HasStateAuthority || _spawnedPlayers.Contains(player))
            return;

        Vector3 spawnPos = GetRandomMapSpawnPosition();
        _usedSpawnPositions.Add(spawnPos);
        _spawnedPlayers.Add(player);

        // Kiểm tra xem người chơi đã có sẵn NetworkPlayer mang từ LobbyScene sang chưa
        NetworkObject existingPlayerObj = Runner.GetPlayerObject(player);
        if (existingPlayerObj == null)
        {
            foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if (np.Object != null && np.Object.InputAuthority == player)
                {
                    existingPlayerObj = np.Object;
                    Runner.SetPlayerObject(player, existingPlayerObj);
                    break;
                }
            }
        }

        if (existingPlayerObj != null)
        {
            if (existingPlayerObj.TryGetComponent<SimpleKCC>(out var kcc))
            {
                kcc.enabled = true;
                kcc.SetPosition(spawnPos);
            }
            else
            {
                existingPlayerObj.transform.position = spawnPos;
            }

            PlayerCount++;
            Debug.Log($"[GameManager] Đã dịch chuyển người chơi {player} từ Lobby tới vị trí: {spawnPos}");
            return;
        }

        // Chỉ spawn mới nếu chạy trực tiếp DungTestScene (không đi qua LobbyScene)
        Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player, (runner, obj) =>
        {
            obj.name = player.ToString();
            runner.SetPlayerObject(player, obj);

            if (obj.TryGetComponent<SimpleKCC>(out var kcc))
            {
                kcc.SetPosition(spawnPos);
            }

            if (LobbyCharacterSelector.SelectedByPlayer.TryGetValue(player, out int selectedCharIndex)
                && obj.TryGetComponent<LobbyCharacterSelector>(out var selector))
            {
                selector.SelectedCharacterIndex = selectedCharIndex;
            }
        });

        PlayerCount++;
        Debug.Log($"[GameManager] Đã spawn mới người chơi {player} tại vị trí ngẫu nhiên trên map: {spawnPos}");
    }
    private Vector3 GetRandomMapSpawnPosition()
    {
        if (mapManager != null)
        {
            Bounds playerBounds = new Bounds(Vector3.zero, new Vector3(1.2f, 2f, 1.2f));
            const float minPlayerSpacing = 2.5f;

            for (int attempt = 0; attempt < 25; attempt++)
            {
                Vector3? candidate = mapManager.FindValidSpawnPositionInGreen(playerBounds, obstacleMap);
                if (!candidate.HasValue) continue;

                Vector3 pos = candidate.Value;
                pos.y = 0f;

                bool tooCloseToOtherPlayer = false;
                foreach (var usedPos in _usedSpawnPositions)
                {
                    if (Vector3.Distance(pos, usedPos) < minPlayerSpacing)
                    {
                        tooCloseToOtherPlayer = true;
                        break;
                    }
                }

                if (!tooCloseToOtherPlayer)
                    return pos;
            }

            // Fallback nếu map ít vùng Green: lấy ngẫu nhiên trong phạm vi mapWidth x mapHeight
            return new Vector3(
                UnityEngine.Random.Range(2f, Mathf.Max(3f, mapManager.mapWidth - 2f)),
                0f,
                UnityEngine.Random.Range(2f, Mathf.Max(3f, mapManager.mapHeight - 2f))
            );
        }

        return SpawnPoint();
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
            StartCountdown(waitingTimer);
    }

    private void StartCountdown(float duration)
    {
        CountDownTimer = TickTimer.CreateFromSeconds(Runner, duration);
        SetState(GameState.Countdown);
    }

    private void HandleCountdown()
    {
        if (CountDownTimer.Expired(Runner))
            StartMatch();
    }

    private void StartMatch()
    {
        Debug.Log("Match Start");
        SetState(GameState.Playing);
    }

    private void HandlePlaying() { }

    private void HandleMatchEnd() { }

    public void PlayerJoined(PlayerRef player)
    {
        //if (HasStateAuthority && Runner.LocalPlayer != player)
        //{
        //    Runner.Spawn(playerPrefab, shopRef.transform.Find("SpawnPoint").position.RandomAround(1f), Quaternion.identity, player, (runner, obj) =>
        //    {
        //        obj.name = player.ToString();
        //        runner.SetPlayerObject(player, obj);
        //    });
        //    PlayerCount++;
        //}
        if (HasStateAuthority && !_spawnedPlayers.Contains(player))
        {
            SpawnPlayerAtRandomMapPosition(player);
        }
    }

    public Vector3 SpawnPoint()
    {
        return shopRef.transform.Find("SpawnPoint").position.RandomAround(1f);
    }

    public void ShowGameOver()
    {
        gameOverUI?.SetActive(true);
    }

    public void KilledPlayer(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        Runner.Despawn(Runner.GetPlayerObject(player));
    }

    public void RegisterCamera(PlayerRef player, Camera camera)
    {
        if (camera == null) return;
        camPlayers[player] = camera;
    }

    public void UnregisterCamera(PlayerRef player)
    {
        camPlayers.Remove(player);
    }

    // =========================================================
    //  DOOR DEATH → Player bị loại
    // =========================================================

    /// <summary>
    /// Gọi từ Door sau khi camera đã MoveToView xong.
    /// Chỉ local player bị loại mới vào đây.
    /// </summary>
    public void OnPlayerEliminated(PlayerRef eliminatedPlayer)
    {
        // Chỉ local player bị loại mới xử lý UI + camera
        if (Runner.LocalPlayer != eliminatedPlayer)
            return;

        // 1. Detach Main Camera trước khi despawn → màn hình không bị đứng
        var playerObj = Runner.GetPlayerObject(eliminatedPlayer);
        if (playerObj != null)
        {
            var camCtrl = playerObj.GetComponentInChildren<PlayerCameraController>();
            if (camCtrl != null)
            {
                spectatorMainCamera = camCtrl.DetachMainCameraForSpectator();
            }
        }

        // 2. Hiện UI bị loại
        if (eliminatedUI != null)
            eliminatedUI.SetActive(true);

        // 3. Bật CameraSwitcher (spectator mode)
        if (cameraSwitcher != null)
        {
            cameraSwitcher.StartSpectatorMode();
        }

        // 4. Despawn Player + Door (sau khi đã detach camera)
        if (HasStateAuthority)
        {
            DespawnEliminatedPlayer(eliminatedPlayer);
        }
        else
        {
            RPC_RequestDespawnEliminatedPlayer(eliminatedPlayer);
        }
    }

    private void DespawnEliminatedPlayer(PlayerRef eliminatedPlayer)
    {
        var playerObj = Runner.GetPlayerObject(eliminatedPlayer);
        if (playerObj != null)
            Runner.Despawn(playerObj);

        foreach (var door in FindObjectsOfType<Door>())
        {
            if (door.Object != null && door.Object.InputAuthority == eliminatedPlayer)
                Runner.Despawn(door.Object);
        }

        // Báo cho các spectator khác cập nhật list (nếu cần)
        if (cameraSwitcher != null)
            cameraSwitcher.OnPlayerLeftOrDied(eliminatedPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDespawnEliminatedPlayer(PlayerRef eliminatedPlayer)
    {
        DespawnEliminatedPlayer(eliminatedPlayer);
    }

    // =========================================================
    //  MONSTER DEATH
    // =========================================================

    /// <summary>
    /// Gọi khi Monster chết (bạn tự hook).
    /// Camera của mọi người zoom về quái → đợi xong → mới hiện UI.
    /// </summary>
    public void OnMonsterDead(Vector3 monsterPosition, PlayerRef killer)
    {
        if (!HasStateAuthority) return;
        RPC_OnMonsterDead(monsterPosition, killer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnMonsterDead(Vector3 monsterPosition, PlayerRef killer)
    {
        HandleMonsterDeadLocalAsync(monsterPosition, killer).Forget();
    }

    private async UniTaskVoid HandleMonsterDeadLocalAsync(Vector3 monsterPosition, PlayerRef killer)
    {
        var playerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        PlayerCameraController camCtrl = null;

        if (playerObj != null)
            camCtrl = playerObj.GetComponentInChildren<PlayerCameraController>();

        // Camera di chuyển về Monster
        if (camCtrl != null)
            await camCtrl.MoveToViewAsync(monsterPosition, monsterZoomDuration);
        else
            await UniTask.Delay(System.TimeSpan.FromSeconds(monsterZoomDuration));

        if(camCtrl != null)
        {
            camCtrl.DetachMainCameraForSpectator();
        }
        // Hiện UI
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        if (killer != PlayerRef.None && Runner.LocalPlayer == killer)
        {
            if (winUI != null)
                winUI.SetActive(true);
        }

        if (HasStateAuthority)
            SetState(GameState.MatchEnd);
    }
}
