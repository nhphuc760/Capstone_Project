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
    Waiting,// đang trong trạng thái chờ chưa khởi tạo session
    Full,
   InitError, // khởi tạo thất bại
   Ready, // trạng thái khởi tạo session hoàn tất có thể join
   InGame, //đang trong match
   Canceled // phòng đã hủy, ví dụ khi mời người khác nhưng chưa kịp nhận câu trả lời đã vào phòng khác, chỉ update khi nào thoát phòng thì update waiting
}

