using UnityEngine;
using UnityEngine.SceneManagement;

public class InviteUIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] InviteElementUI inviteElementUIPrefab;
    [SerializeField] RectTransform container;
    EventBinding<InviteAddedArgs> inviteAddedBinding;
    void Start()
    {
        inviteAddedBinding = new EventBinding<InviteAddedArgs>(ShowPopup);
        EventBus<InviteAddedArgs>.Register(inviteAddedBinding);
    }


    async void ShowPopup(InviteAddedArgs inviteArgs) // checked
    {
        var popupUI = Instantiate(inviteElementUIPrefab, container);
        Sprite avt = NetworkDataManager.Instance.friendManager.GetAvatar(inviteArgs.invite.SenderID);
        var result = await popupUI.Show("Đã gửi lời mời vào đội", avt);
        // Kiểm tra phòng hợp lệ không => có thì vào luôn và gửi token, chưa thì đợi phòng ready
        if (result)
        {
            //
            var roomSnapshot = await FirebaseManager.RealtimeDB.GetValue($"Lobbies/{inviteArgs.invite.RoomID}");
            if (!roomSnapshot.Exists) { Debug.Log("Room invalid"); return; }
            var room = JsonUtility.FromJson<Room>(roomSnapshot.GetRawJsonValue());
            switch ((RoomStatus)room.status)
            {
                case RoomStatus.Waiting:
                    await InviteDatabase.UpdateStatus(inviteArgs.invite.RoomID, InviteStatus.Accepted);
                    break;
                case RoomStatus.InGame:
                    Debug.Log("Không thể tham gia, phòng đã bắt đầu trận đấu");
                    break;
                case RoomStatus.Ready:
                    //Join
                    break;
                case RoomStatus.Canceled:
                    Debug.Log("Phòng không tồn tại");
                    break;
                case RoomStatus.InitError:
                    Debug.Log("Lỗi từ phía máy chủ, phòng chưa sẵn sàng");
                    break;
            }


        }
        else
        {
            await InviteDatabase.UpdateStatus(inviteArgs.invite.SenderID, InviteStatus.Rejected);
        }

    }
    private void OnDestroy()
    {
        EventBus<InviteAddedArgs>.Deregister(inviteAddedBinding);
    }

}

public struct InviteAddedArgs : IEvent 
{
    public Invite invite;
}

