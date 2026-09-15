// using Fusion;
// using UnityEngine;

// public class TradeSessionTest : MonoBehaviour
// {
//     [Header("Trade Session")]
//     [SerializeField] private TradeSession tradeSession;

//     private NetworkObject networkObject;

//     private string bidInput = "";

//     private void Awake()
//     {
//         networkObject = GetComponent<NetworkObject>();

//         if (tradeSession == null)
//         {
//             tradeSession = FindFirstObjectByType<TradeSession>();
//         }

//         Debug.Log(
//             $"[TradeSessionTest] Awake | " +
//             $"NetworkObject={networkObject} | " +
//             $"TradeSession={tradeSession}"
//         );
//     }

//     private void Update()
//     {
//         if (networkObject == null)
//             return;

//         // Chỉ Player local mới nhận input
//         if (!networkObject.HasInputAuthority)
//             return;

//         HandleTradeInput();
//     }

//     private void HandleTradeInput()
//     {
//         // T = Request Start Trade Session
//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             RequestStartSession();
//         }

//         // X = Cancel
//         if (Input.GetKeyDown(KeyCode.X))
//         {
//             CancelSession();
//         }

//         // C = Complete
//         if (Input.GetKeyDown(KeyCode.C))
//         {
//             CompleteSession();
//         }

//         // Nhập bid
//         HandleBidInput();
//     }

//     // =========================================================
//     // START SESSION
//     // =========================================================

//     private void RequestStartSession()
//     {
//         if (tradeSession == null)
//         {
//             Debug.LogError(
//                 "[TradeSessionTest] TradeSession is NULL!"
//             );

//             return;
//         }

//         if (tradeSession.State != TradeSessionState.None)
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] Cannot start session. " +
//                 $"Current State={tradeSession.State}"
//             );

//             return;
//         }

//         tradeSession.RPC_RequestStartTestSession();

//         Debug.Log(
//             $"[TradeSessionTest] Request Start Trade Session"
//         );
//     }

//     // =========================================================
//     // BID INPUT
//     // =========================================================

//     private void HandleBidInput()
//     {
//         // Số 0-9
//         for (KeyCode key = KeyCode.Alpha0;
//              key <= KeyCode.Alpha9;
//              key++)
//         {
//             if (!Input.GetKeyDown(key))
//                 continue;

//             int number = key - KeyCode.Alpha0;

//             if (bidInput.Length >= 3)
//                 return;

//             bidInput += number.ToString();

//             Debug.Log(
//                 $"[TradeSessionTest] Current Bid Input = {bidInput}"
//             );

//             return;
//         }

//         // Backspace
//         if (Input.GetKeyDown(KeyCode.Backspace))
//         {
//             if (bidInput.Length > 0)
//             {
//                 bidInput =
//                     bidInput.Substring(
//                         0,
//                         bidInput.Length - 1
//                     );
//             }

//             Debug.Log(
//                 $"[TradeSessionTest] Current Bid Input = {bidInput}"
//             );

//             return;
//         }

//         // Enter = Submit Bid
//         if (Input.GetKeyDown(KeyCode.Return) ||
//             Input.GetKeyDown(KeyCode.KeypadEnter))
//         {
//             SubmitBid();
//         }
//     }

//     // =========================================================
//     // SUBMIT BID
//     // =========================================================

//     private void SubmitBid()
//     {
//         if (tradeSession == null)
//         {
//             Debug.LogError(
//                 "[TradeSessionTest] TradeSession is NULL!"
//             );

//             return;
//         }

//         if (string.IsNullOrEmpty(bidInput))
//         {
//             Debug.LogWarning(
//                 "[TradeSessionTest] Bid input is empty."
//             );

//             return;
//         }

//         if (!int.TryParse(bidInput, out int bidAmount))
//         {
//             Debug.LogWarning(
//                 "[TradeSessionTest] Invalid bid input."
//             );

//             bidInput = "";
//             return;
//         }

//         // Bid phải từ 10 đến 100
//         if (bidAmount < 10 || bidAmount > 100)
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] Bid must be between 10 and 100. " +
//                 $"Input={bidAmount}"
//             );

//             bidInput = "";
//             return;
//         }

//         if (!tradeSession.IsActive)
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] TradeSession is not active."
//             );

//             bidInput = "";
//             return;
//         }

//         if (!tradeSession.CanBid(bidAmount))
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] Bid must be higher than " +
//                 $"CurrentBid={tradeSession.CurrentBid}. " +
//                 $"Input={bidAmount}"
//             );

//             bidInput = "";
//             return;
//         }

//         // Gửi request lên State Authority
//         tradeSession.RPC_PlaceBid(bidAmount);

//         Debug.Log(
//             $"[TradeSessionTest] Bid submitted | " +
//             $"Bid={bidAmount}"
//         );

//         bidInput = "";
//     }

//     // =========================================================
//     // COMPLETE
//     // =========================================================

//     private void CompleteSession()
//     {
//         if (tradeSession == null)
//             return;

//         if (!tradeSession.IsActive)
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] Cannot complete. " +
//                 $"State={tradeSession.State}"
//             );

//             return;
//         }

//         tradeSession.RPC_CompleteSession();

//         Debug.Log(
//             "[TradeSessionTest] Request Complete Trade Session"
//         );
//     }

//     // =========================================================
//     // CANCEL
//     // =========================================================

//     private void CancelSession()
//     {
//         if (tradeSession == null)
//             return;

//         if (!tradeSession.IsActive)
//         {
//             Debug.LogWarning(
//                 $"[TradeSessionTest] Cannot cancel. " +
//                 $"State={tradeSession.State}"
//             );

//             return;
//         }

//         tradeSession.RPC_CancelSession();

//         Debug.Log(
//             "[TradeSessionTest] Request Cancel Trade Session"
//         );
//     }
// }

using Fusion;
using UnityEngine;

public class TradeSessionTest : MonoBehaviour
{
    [SerializeField] private TradeSession tradeSession;

    private string bidInput = "";

    private void Update()
    {
        // Test keyboard có hoạt động không
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[TradeTest] T pressed");

            if (tradeSession == null)
            {
                Debug.LogError("[TradeTest] TradeSession is NULL");
                return;
            }

            Debug.Log($"[TradeTest] Session State: {tradeSession.State}");

            if (tradeSession.State != TradeSessionState.Active)
            {
                Debug.LogWarning("[TradeTest] Trade Session is not Active");
                return;
            }

            Debug.Log("[TradeTest] Trade Session is Active");
            Debug.Log("[TradeTest] Enter bid using number keys");
        }

        // Nhập số 0-9
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                bidInput += i;
                Debug.Log($"[TradeTest] Bid Input: {bidInput}");
            }
        }

        // Xóa số
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (bidInput.Length > 0)
            {
                bidInput = bidInput.Substring(0, bidInput.Length - 1);
                Debug.Log($"[TradeTest] Bid Input: {bidInput}");
            }
        }

        // Enter để bid
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SubmitBid();
        }
    }

    private void SubmitBid()
    {
        Debug.Log($"[TradeTest] Enter pressed | Input = {bidInput}");

        if (string.IsNullOrEmpty(bidInput))
        {
            Debug.LogWarning("[TradeTest] No bid entered");
            return;
        }

        if (!int.TryParse(bidInput, out int bidAmount))
        {
            Debug.LogWarning("[TradeTest] Invalid bid");
            bidInput = "";
            return;
        }

        Debug.Log($"[TradeTest] Trying to bid: {bidAmount}");

        if (bidAmount < 10 || bidAmount > 100)
        {
            Debug.LogWarning("[TradeTest] Bid must be between 10 and 100");
            bidInput = "";
            return;
        }

        if (tradeSession == null)
        {
            Debug.LogError("[TradeTest] TradeSession is NULL");
            return;
        }

        if (tradeSession.State != TradeSessionState.Active)
        {
            Debug.LogWarning("[TradeTest] Trade Session is not Active");
            bidInput = "";
            return;
        }

        if (bidAmount <= tradeSession.CurrentBid)
        {
            Debug.LogWarning(
                $"[TradeTest] Bid too low! Current Bid = {tradeSession.CurrentBid}"
            );

            bidInput = "";
            return;
        }

        Debug.Log($"[TradeTest] Sending RPC_PlaceBid({bidAmount})");

        tradeSession.RPC_PlaceBid(bidAmount);

        bidInput = "";
    }
}