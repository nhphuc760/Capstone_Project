using System;
using System.Collections.Generic;

[Serializable]
public class Room
{
    public string HostID;
    public string RoomName;
    public int PlayerCount;
    public int Status;
}

public enum RoomStatus
{   
    Full,
    Ready, // trạng thái khởi tạo session hoàn tất có thể join
    InGame, //đang trong match    
    InitError, // khởi tạo thất bại
}

