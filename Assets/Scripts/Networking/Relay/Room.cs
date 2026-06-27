using System;
using System.Collections.Generic;

[Serializable]
public class Room
{
    public string hostID;
    public string roomName;
    public int playerCount;
    public int status;
}

public enum RoomStatus
{
    Waiting,// đang trong trạng thái chờ chưa khởi tạo session
   InitError, // khởi tạo thất bại
   Ready, // trạng thái khởi tạo session hoàn tất có thể join
   InGame, //đang trong match
   Canceled // phòng đã hủy, ví dụ khi mời người khác nhưng chưa kịp nhận câu trả lời đã vào phòng khác, chỉ update khi nào thoát phòng thì update waiting
}

