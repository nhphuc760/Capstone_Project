using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] FirstTimeSetup firstTimeSetup;


    private void Awake()
    {
        // Bước 1: Kiểm tra các dependency (thư viện hệ thống phụ thuộc) trên thiết bị
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {


            var dependencyStatus = task.Result;

            // Bước 2: Nếu mọi thứ sẵn sàng (Available), Firebase sẽ tự động khởi tạo DefaultInstance
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Khởi tạo thành công! Bạn có thể gán Instance ra biến để dùng toàn cục
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Debug.Log("Firebase đã khởi tạo thành công và sẵn sàng sử dụng!");

                // Kích hoạt các logic tiếp theo tại đây (ví dụ: Tự động đăng nhập)
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                FirebaseManager.RealtimeDB.Init();
                OnFirebaseReady();
            }
            else
            {
                // Thất bại (Có thể do thiết bị thiếu Google Play Services và không thể tự fix)
                Debug.LogError($"Không thể khởi tạo Firebase: {dependencyStatus}");
            }
        });
    }


    private async void OnFirebaseReady()
    {      
        var data = await FirebaseManager.RealtimeDB.GetValue($"Users/{FirebaseManager.UserID}");       
        if (data.Exists)
        {
            string jsonUser = data.GetRawJsonValue();
            Debug.Log("Bootstrap-JsonUser: " + jsonUser);            
            await NetworkDataManager.Instance.Initialize(jsonUser);
            await SceneController.Instance.NewTransitionPlan()
                                   .Load(new ParameterScene { Name = SceneDatabase.LOBBY }, true)
                                   .UnLoad(new ParameterScene { Name = SceneDatabase.MAINMENU })
                                   .WithFadeOut()
                                   .Perform();
        }
        else
        {
            firstTimeSetup.gameObject.SetActive(true);
            await SceneController.Instance.loadingOverlay.FadeOutBlack(.5f);
        }
    }
}
