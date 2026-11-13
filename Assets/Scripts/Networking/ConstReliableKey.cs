using Fusion.Sockets;
using UnityEngine;

public class ConstReliableKey
{
    public static readonly ReliableKey AuctionResult = ReliableKey.FromInts(1, 0, 0, 0);
    public static readonly ReliableKey InteractionResult = ReliableKey.FromInts(2, 0, 0, 0);

}
