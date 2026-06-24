using System;

[Serializable]
public struct Room
{
    public string hostID;
    public RoomStatus roomStatus;
    public string roomName;
}

[Serializable]
public struct RoomStatus
{
    public VirtualRoomState roomState;
    public int currentPlayerCount;
    public int maxPlayerCount;
    public DateTime createAt; 
}




public enum VirtualRoomState
{
   WaitingForGuests,
   StartingPhotonSession,
   InGame,
   Canceled
}

