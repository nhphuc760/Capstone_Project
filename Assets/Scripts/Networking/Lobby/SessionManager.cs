using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Fusion;
using Newtonsoft.Json;
using UnityEngine;

public class SessionManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] Transform[] _slots;
    [SerializeField] PlayerPreviewLocal playerPreviewLocal;
    [SerializeField] PlayerPreviewNetwork playerPreviewNetwork;

    [Header("Character Prefabs (Thêm dần nhân vật vào đây)")]
    [SerializeField] private NetworkObject[] _characterPrefabs;

    PlayerPreviewLocal _currentLocalPreview;
    ILobbyLayoutStrategy _layoutStrategy;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> _playerSlotIndices => default;
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkString<_32>> _userIDs => default;
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> _playerCharacterIndices => default;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayerObjects = new Dictionary<PlayerRef, NetworkObject>();

    public static SessionManager Ins { get; private set; }

    public bool IsSpawned { get; private set; } = false;
    public int CharacterCount => _characterPrefabs != null ? _characterPrefabs.Length : 0;

    public override void Spawned()
    {
        Debug.Log("LobbyManager Spawned called");
        HideLocalPreview();
        InitializeLayoutStrategy(Runner.SessionInfo.MaxPlayers);
        if (HasStateAuthority)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                if (!_playerSlotIndices.ContainsKey(player))
                {
                    HandlePlayerJoined(player);
                }
            }
        }
        IsSpawned = true;
        if (NetworkDataManager.Instance != null)
            NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.InParty).Forget();
    }

    private void Awake()
    {
        Ins = this;
        ShowLocalPreview();
    }

    public void InitializeLayoutStrategy(int maxPlayers)
    {
        // Chọn Strategy theo Mode
        _layoutStrategy = maxPlayers switch
        {
            2 => new TwoPlayerLayoutStrategy(),
            4 => new FourPlayerLayoutStrategy(fillLeftFirst: true),
            _ => FallbackStrategy(maxPlayers)
        };
    }

    ILobbyLayoutStrategy FallbackStrategy(int maxPlayers)
    {
        Debug.LogWarning($"[LobbyManager] MaxPlayers ({maxPlayers}) không khớp cấu hình chuẩn (2 hoặc 4). Mặc định dùng 4-player layout.");
        return new FourPlayerLayoutStrategy(fillLeftFirst: true);
    }

    private void ShowLocalPreview()
    {
        if (playerPreviewLocal != null)
            _currentLocalPreview = Instantiate(playerPreviewLocal, Vector3.zero, Quaternion.identity);
    }

    private void HideLocalPreview()
    {
        if (_currentLocalPreview != null)
        {
            foreach (SkinnedMeshRenderer smr in _currentLocalPreview.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.sharedMesh = null;
                smr.sharedMaterials = System.Array.Empty<Material>();
                Destroy(smr);
            }
            Destroy(_currentLocalPreview.gameObject);
            _currentLocalPreview = null;
        }
    }

    public Transform GetSpawnPoint(int index)
    {
        return _layoutStrategy.GetSpawnPoint(index, _slots);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        Debug.Log("Player join called");
        // Xóa Local Preview nếu đây là người đầu tiên (Host) kết nối thành công
        if (!_playerSlotIndices.ContainsKey(player))
        {
            HandlePlayerJoined(player);
            var token = Runner.GetPlayerConnectionToken(player);
            if (token != null && RoomDatabaseManager.Instance != null && _userIDs.ContainsKey(player))
            {
                RoomDatabaseManager.Instance.AddMembers(_userIDs[player].Value).Forget();
            }
        }
    }

    void HandlePlayerJoined(PlayerRef player)
    {
        Debug.Log("HandlePlayer joined called");
        int assignedIndex = GetAvailableSlotIndex();
        _playerSlotIndices.Add(player, assignedIndex);

        int defaultCharIndex = 0;
        if (!_playerCharacterIndices.ContainsKey(player))
            _playerCharacterIndices.Add(player, defaultCharIndex);
        else
            _playerCharacterIndices.Set(player, defaultCharIndex);

        SpawnCharacterForPlayer(player, assignedIndex, defaultCharIndex);
    }

    private void SpawnCharacterForPlayer(PlayerRef player, int slotIndex, int characterIndex)
    {
        if (!HasStateAuthority) return;

        // Xóa prefab cũ của player này nếu đang đổi nhân vật
        if (_spawnedPlayerObjects.TryGetValue(player, out NetworkObject oldObj) && oldObj != null)
        {
            Runner.Despawn(oldObj);
            _spawnedPlayerObjects.Remove(player);
        }

        Transform spawnPoint = _layoutStrategy.GetSpawnPoint(slotIndex, _slots);
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Ưu tiên lấy Prefab nhân vật thật từ _characterPrefabs; nếu chưa gán thì dùng tạm playerPreviewNetwork
        NetworkObject spawnedObj = null;
        if (_characterPrefabs != null && _characterPrefabs.Length > 0)
        {
            int safeIndex = Mathf.Clamp(characterIndex, 0, _characterPrefabs.Length - 1);
            NetworkObject selectedPrefab = _characterPrefabs[safeIndex];

            if (selectedPrefab != null)
            {
                spawnedObj = Runner.Spawn(selectedPrefab, spawnPos, spawnRot, player, onBeforeSpawned: (runner, obj) =>
                {
                    PrepareLobbyCharacterInstance(obj);
                });
            }
        }
        else if (playerPreviewNetwork != null)
        {
            spawnedObj = Runner.Spawn(playerPreviewNetwork, spawnPos, spawnRot, player, onBeforeSpawned: (runner, obj) =>
            {
            }).Object;
        }

        if (spawnedObj != null)
        {
            _spawnedPlayerObjects[player] = spawnedObj;
        }
    }

    private void PrepareLobbyCharacterInstance(NetworkObject obj)
    {
        // Đảm bảo nhân vật đứng yên tạo dáng ở Lobby, không bị rơi tự do nếu chưa có sàn Collider
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private int GetAvailableSlotIndex()
    {
        int max = _slots != null ? _slots.Length : 4;
        for (int i = 0; i < max; i++)
        {
            bool isTaken = false;
            foreach (var kvp in _playerSlotIndices)
            {
                if (kvp.Value == i)
                {
                    isTaken = true;
                    break;
                }
            }
            if (!isTaken) return i;
        }
        return _playerSlotIndices.Count;
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        Debug.Log("PlayerLeft called");

        if (_spawnedPlayerObjects.TryGetValue(player, out NetworkObject leftObj) && leftObj != null)
        {
            Runner.Despawn(leftObj);
            _spawnedPlayerObjects.Remove(player);
        }

        if (_playerSlotIndices.ContainsKey(player))
        {
            _playerSlotIndices.Remove(player);
            if (_playerCharacterIndices.ContainsKey(player))
                _playerCharacterIndices.Remove(player);

            if (RoomDatabaseManager.Instance != null && _userIDs.ContainsKey(player))
                RoomDatabaseManager.Instance.RemoveMembers(_userIDs[player].Value).Forget();
        }
    }

    #region API sẵn sàng cho nút mũi tên < > chọn nhân vật sau này
    public void SelectNextCharacter()
    {
        ChangeCharacterByStep(1);
    }

    public void SelectPreviousCharacter()
    {
        ChangeCharacterByStep(-1);
    }

    private void ChangeCharacterByStep(int step)
    {
        if (Runner == null || !IsSpawned || _characterPrefabs == null || _characterPrefabs.Length <= 1)
            return;

        PlayerRef localPlayer = Runner.LocalPlayer;
        int current = _playerCharacterIndices.ContainsKey(localPlayer) ? _playerCharacterIndices[localPlayer] : 0;
        int next = (current + step + _characterPrefabs.Length) % _characterPrefabs.Length;

        RPC_RequestSelectCharacter(localPlayer, next);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSelectCharacter(PlayerRef player, int characterIndex)
    {
        if (!HasStateAuthority || _characterPrefabs == null || _characterPrefabs.Length == 0)
            return;

        int safeIndex = Mathf.Clamp(characterIndex, 0, _characterPrefabs.Length - 1);

        if (_playerCharacterIndices.ContainsKey(player))
            _playerCharacterIndices.Set(player, safeIndex);
        else
            _playerCharacterIndices.Add(player, safeIndex);

        int slotIndex = _playerSlotIndices.ContainsKey(player) ? _playerSlotIndices[player] : 0;
        SpawnCharacterForPlayer(player, slotIndex, safeIndex);
    }
    #endregion
}

public interface ILobbyLayoutStrategy
{
    Transform GetSpawnPoint(int playerIndex, Transform[] slots);
}

public class TwoPlayerLayoutStrategy : ILobbyLayoutStrategy
{
    public Transform GetSpawnPoint(int playerIndex, Transform[] slots)
    {
        //return playerIndex switch
        //{
        //    //0 => slots[1], // Host -> Pos 2
        //    //1 => slots[2], // Client 1 -> Pos 3
        //    //_ => slots[1]
        //};
        if (slots == null || slots.Length == 0) return null;
        int safeIndex = Mathf.Clamp(playerIndex, 0, slots.Length - 1);
        return slots[safeIndex];
    }
}

//public class FourPlayerLayoutStrategy : ILobbyLayoutStrategy
//{
//    readonly bool _fillLeftFirst;
//    public FourPlayerLayoutStrategy(bool fillLeftFirst = true)
//    {
//        _fillLeftFirst = fillLeftFirst;
//    }
//    public Transform GetSpawnPoint(int playerIndex, Transform[] slots)
//    {
//        return playerIndex switch
//        {
//            0 => slots[1], // Host -> Pos 2
//            1 => slots[2], // Player 2 -> Pos 3
//            2 => _fillLeftFirst ? slots[0] : slots[3], // Player 3 -> Pos 1 hoặc Pos 4
//            3 => _fillLeftFirst ? slots[3] : slots[0], // Player 4 -> Pos 4 hoặc Pos 1
//            _ => slots[0]
//        };
//    }
//}
public class FourPlayerLayoutStrategy : ILobbyLayoutStrategy
{
    public FourPlayerLayoutStrategy(bool fillLeftFirst = true) { }

    public Transform GetSpawnPoint(int playerIndex, Transform[] slots)
    {
        if (slots == null || slots.Length == 0) return null;
        int safeIndex = Mathf.Clamp(playerIndex, 0, slots.Length - 1);
        return slots[safeIndex]; // 0 -> Pos 1, 1 -> Pos 2, 2 -> Pos 3, 3 -> Pos 4
    }
}