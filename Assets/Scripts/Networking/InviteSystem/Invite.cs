using System;

[Serializable]
public class Invite
{
    public Invite() { }
    public string SenderID;
    public string RoomID;   // Firebase Push Key (dùng để đọc Lobbies/{RoomID})
    public string RoomCode; // Mã phòng 5 ký tự (dùng cho FusionBootstrap Join)
    public InviteStatus Status;
    public long CreateAt;
}

public enum InviteStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}