using System;
using System.Collections.Generic;

[Serializable]
public class Room
{
    public string RoomID;
    public string HostID;
    public string RoomName;
    public int MaxPlayerCount;
    public RoomStatus Status;
    public List<string> Members; // danh sách các thành viên trong phòng
}

public enum RoomStatus
{   
    Full,
    Ready, // trạng thái khởi tạo session hoàn tất có thể join
    InGame, //đang trong match
    Error, // trạng thái lỗi
}

