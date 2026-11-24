using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1000)]
public class EventSystemGuard : MonoBehaviour
{
    private void Awake()
    {
        // Khi đi từ MainMenu / Lobby sang đã có sẵn EventSystem của SceneController
        if (SceneController.Instance != null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Chỉ giữ lại EventSystem cục bộ khi bấm Play test lẻ trực tiếp tại scene này
        var myEventSystem = GetComponent<EventSystem>();
        if (myEventSystem != null)
        {
            EventSystem.current = myEventSystem;
        }
    }
}