using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class AnonymousLogin : MonoBehaviour
{
    public GameObject loginBTN, successLoginPopup;


    private async void Start()
    {
        // Bước 1: Kiểm tra các dependency (thư viện hệ thống phụ thuộc) trên thiết bị
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            Debug.Log("Firebase đã khởi tạo thành công và sẵn sàng sử dụng!");
            await AnonymousLoginBTN();
        }
        else
        {
            // Thất bại (Có thể do thiết bị thiếu Google Play Services và không thể tự fix)
            Debug.LogError($"Không thể khởi tạo Firebase: {status}");
        }      

    }

    public async void Login()
    {
        await AnonymousLoginBTN();
    }

    async Task AnonymousLoginBTN()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if(auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log("SignOut");
        }
        await auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("Anonymous sign-in was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Anonymous sign-in encountered an error: " + task.Exception);
                return;
            }

            print("Login Sucess");

            AuthResult result = task.Result;
            print("User ID: " + result.User.UserId);
            //print("User Name: " + result.User.DisplayName);
            //can save user id in playerprefs
            //GuestLoginSuccess(result.User.UserId);
        });
        EventBus<EventTest.OnLoginSuccess>.Raise(new EventTest.OnLoginSuccess { });
    }

    void GuestLoginSuccess(string userId)
    {
        loginBTN.SetActive(false);
        successLoginPopup.SetActive(true);
        successLoginPopup.transform.Find("Disc").GetComponent<TextMeshProUGUI>().text = "Login Success\nUser ID: " + userId;
    }
}

public class EventTest
{
    public struct OnLoginSuccess :IEvent
    { 

    }

}
