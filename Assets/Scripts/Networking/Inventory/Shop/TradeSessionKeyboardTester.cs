using System.Linq;
using Fusion;
using UnityEngine;

public class TradeSessionKeyboardTester : NetworkBehaviour
{
    [Header("References")]
    [SerializeField]
    private TradeSession tradeSession;

    [Header("Test Item")]
    [SerializeField]
    private int itemID = 1;

    [SerializeField]
    private int itemAmount = 1;

    [Header("Test Bids")]
    [SerializeField]
    private int playerABid = 100;

    [SerializeField]
    private int playerBBid = 150;

    [SerializeField]
    private bool logPlayerDiagnostics = true;

    private PlayerRef[] players;

    public override void Spawned()
    {
        base.Spawned();

        if (tradeSession == null)
        {
            tradeSession = GetComponent<TradeSession>();
        }
    }

    private void Update()
    {
        if (Runner == null || !Object.HasStateAuthority)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaceTestBid(0, playerABid);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlaceTestBid(1, playerBBid);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            FinishTestAuction();
        }
    }

    private bool PrepareAuction()
    {
        if (tradeSession == null)
        {
            Debug.LogError($"{name}: TradeSession is missing.");
            return false;
        }

        players = Runner.ActivePlayers.ToArray();

        if (logPlayerDiagnostics)
        {
            LogPlayerDiagnostics();
        }

        if (players.Length < 2)
        {
            Debug.LogWarning("Trade test needs 2 players in session.");
            return false;
        }

        if (tradeSession.State == TradeSession.SessionState.Waiting ||
            tradeSession.State == TradeSession.SessionState.Finished ||
            tradeSession.State == TradeSession.SessionState.Cancelled)
        {
            if (!tradeSession.Setup(players[0], players[1], itemID, itemAmount))
            {
                Debug.LogWarning("Trade test setup failed.");
                return false;
            }
        }

        if (tradeSession.State == TradeSession.SessionState.Ready)
        {
            if (!tradeSession.StartAuction())
            {
                Debug.LogWarning("Trade test start failed.");
                return false;
            }
        }

        return tradeSession.State == TradeSession.SessionState.Running;
    }

    private void PlaceTestBid(int playerIndex, int bidAmount)
    {
        if (!PrepareAuction())
            return;

        PlayerRef player = players[playerIndex];

        if (!tradeSession.PlaceBid(player, bidAmount))
        {
            Debug.LogWarning($"Trade test bid failed. Player={player}, Bid={bidAmount}");
            return;
        }

        Debug.Log($"Trade test bid placed. Player={player}, Bid={bidAmount}");
    }

    private void FinishTestAuction()
    {
        if (tradeSession == null)
            return;

        if (tradeSession.State != TradeSession.SessionState.Running)
        {
            Debug.LogWarning("Trade test auction is not running.");
            return;
        }

        tradeSession.FinishAuction();

        Debug.Log(
            $"Trade test finished. Result={tradeSession.Result}, " +
            $"Winner={tradeSession.Winner}, WinningBid={tradeSession.WinningBid}"
        );
    }

    private void LogPlayerDiagnostics()
    {
        Debug.Log($"Trade test player count: {players.Length}");

        for (int i = 0; i < players.Length; i++)
        {
            PlayerRef player = players[i];

            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            {
                Debug.LogWarning($"Trade test player[{i}] {player}: no player object found.");
                continue;
            }

            NetworkInventory inventory = playerObject.GetComponent<NetworkInventory>();

            if (inventory == null)
            {
                Debug.LogWarning(
                    $"Trade test player[{i}] {player}: object={playerObject.name}, NetworkInventory=missing."
                );
                continue;
            }

            Debug.Log(
                $"Trade test player[{i}] {player}: object={playerObject.name}, " +
                $"NetworkInventory=found, InventoryStateAuthority={inventory.Object.HasStateAuthority}"
            );
        }
    }
}
