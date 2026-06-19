using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class AnonymousLogin : MonoBehaviour
{
    public GameObject loginBTN, successLoginPopup;

    public async void Login()
    {
        await AnonymousLoginBTN();
    }

    async Task AnonymousLoginBTN()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
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
            GuestLoginSuccess(result.User.UserId);
        });
    }

    void GuestLoginSuccess(string userId)
    {
        loginBTN.SetActive(false);
        successLoginPopup.SetActive(true);
        successLoginPopup.transform.Find("Disc").GetComponent<TextMeshProUGUI>().text = "Login Success\nUser ID: " + userId;
    }
}
