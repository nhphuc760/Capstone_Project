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
   Waiting,// đang trong trạng thái chờ chưa khởi tạo session
   InitError, // khởi tạo thất bại
   Ready, // trạng thái khởi tạo session hoàn tất có thể join
   InGame, //đang trong match
   Canceled // phòng đã hủy, ví dụ khi mời người khác nhưng chưa kịp nhận câu trả lời đã vào phòng khác, chỉ update khi nào thoát phòng thì update waiting
}

