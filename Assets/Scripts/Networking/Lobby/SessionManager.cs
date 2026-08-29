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

    PlayerPreviewLocal _currentLocalPreview;
    ILobbyLayoutStrategy _layoutStrategy;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> _playerSlotIndices => default;
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkString<_32>> _userIDs => default;

    public static SessionManager Ins { get; private set; }

    public bool IsSpawned { get; private set; } = false;
    
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
        _currentLocalPreview = Instantiate(playerPreviewLocal, Vector3.zero, Quaternion.identity);
    }

    private void HideLocalPreview()
    {
        if (_currentLocalPreview != null)
        {
           _currentLocalPreview.gameObject.SetActive(false);
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
            if (token != null)
            {
                RoomDatabaseManager.Instance.AddMembers(_userIDs[player].Value).Forget();
            }
        }
    }

    void HandlePlayerJoined(PlayerRef player)
    {
        Debug.Log("HandlePlayer joined called");
        int assignedIndex = _playerSlotIndices.Count;
        _playerSlotIndices.Add(player, assignedIndex);

        Transform spawnPoint = _layoutStrategy.GetSpawnPoint(assignedIndex, _slots);

        // Spawn Player Networked
        Runner.Spawn(playerPreviewNetwork, spawnPoint.position, Quaternion.identity, player, onBeforeSpawned: (runner, obj) => 
        {
          
        });
       
    }


    public void PlayerLeft(PlayerRef player)
    {
        if(!HasStateAuthority) return;
        Debug.Log("PlayerLeft called");
        if (!_playerSlotIndices.ContainsKey(player))
        {
            _playerSlotIndices.Remove(player);
            RoomDatabaseManager.Instance.RemoveMembers(_userIDs[player].Value).Forget();
        }
    }
}

public interface ILobbyLayoutStrategy 
{
    Transform GetSpawnPoint(int playerIndex, Transform[] slots);
}

public class TwoPlayerLayoutStrategy : ILobbyLayoutStrategy
{
    public Transform GetSpawnPoint(int playerIndex, Transform[] slots)
    {
        return playerIndex switch
        {
            0 => slots[1], // Host -> Pos 2
            1 => slots[2], // Client 1 -> Pos 3
            _ => slots[1]
        };
    }
}

public class FourPlayerLayoutStrategy : ILobbyLayoutStrategy
{
    readonly bool _fillLeftFirst;
    public FourPlayerLayoutStrategy(bool fillLeftFirst = true)
    {
        _fillLeftFirst = fillLeftFirst;
    }
    public Transform GetSpawnPoint(int playerIndex, Transform[] slots)
    {
        return playerIndex switch
        {
            0 => slots[1], // Host -> Pos 2
            1 => slots[2], // Player 2 -> Pos 3
            2 => _fillLeftFirst ? slots[0] : slots[3], // Player 3 -> Pos 1 hoặc Pos 4
            3 => _fillLeftFirst ? slots[3] : slots[0], // Player 4 -> Pos 4 hoặc Pos 1
            _ => slots[0]
        };
    }
}