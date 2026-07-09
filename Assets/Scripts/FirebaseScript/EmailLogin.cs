using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase;
using Firebase.Extensions;
using System.Collections;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class EmailLogin : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private TMP_InputField LoginEmail;
    [SerializeField] private TMP_InputField LoginPassword;

    [Header("Sign up")]
    [SerializeField] private TMP_InputField SignUpEmail;
    [SerializeField] private TMP_InputField SignUpPassword;
    [SerializeField] private TMP_InputField SignUpPasswordConfirm;

    [Header("Extra")]
    //public GameObject loadingScreen;
    [SerializeField] private Toggle rememberMeToggle;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI logTxt, wrongEmailPasswordText;
    [SerializeField] private GameObject loginUI, signUpUI, emailVerificationPanel, emailPasswordNotificationPanel;

    [Header("Email List")]
    [SerializeField] private GameObject emailSuggestionPanel;
    [SerializeField] private Transform emailSuggestionContent;
    [SerializeField] private GameObject emailItemPrefab;
    [SerializeField] private EmailSuggestion emailSuggestion;

    [Header("Forgot Password")]
    [SerializeField] private GameObject forgotPasswordPanel;
    [SerializeField] private TMP_InputField forgotPasswordEmail;

    private void Start()
    {
        LoadRememberedEmail();
        emailSuggestionPanel.SetActive(false);

        if (rememberMeToggle != null)
            rememberMeToggle.isOn = false;
    }

    #region  sign up
    // Sign up
    public void SignUp()
    {
        //loadingScreen.SetActive(true);

        // Create a new user with email and password
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        string email = SignUpEmail.text;
        string password = SignUpPassword.text;
        string confirmPassword = SignUpPasswordConfirm.text;

        // Check if the email and password fields are not empty
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignUp was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignUp encountered an error: " + task.Exception);
                return;
            }

            // Sign up successful
            //loadingScreen.SetActive(false);
            AuthResult result = task.Result;
            Debug.LogFormat("User signed up successfully: {0} ({1})", 
                result.User.DisplayName, result.User.UserId);

            SignUpEmail.text = "";
            SignUpPassword.text = "";
            SignUpPasswordConfirm.text = "";

            if (result.User.IsEmailVerified)
            {
                showLogMsg("Sign up successful! Please log in.");
            }
            else
            {

                openNotificationPanel();
                SendEmailVerification();        
            }
        });

        signUpUI.SetActive(false);
        loginUI.SetActive(true);
        
    }

    //Email Confirmation 
    public void SendEmailVerification()
    {
        StartCoroutine(SendEmailVerificationAsync());
    }

    IEnumerator SendEmailVerificationAsync()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            var sendEmailTask = user.SendEmailVerificationAsync();
            yield return new WaitUntil(() => sendEmailTask.IsCompleted);

            if (sendEmailTask.Exception != null)
            {
                print("Email send error");
                FirebaseException firebaseException = sendEmailTask.Exception.GetBaseException() as FirebaseException;
                AuthError error = (AuthError) firebaseException.ErrorCode;

                switch (error)
                {
                    case AuthError.None: break;
                    case AuthError.Unimplemented: break;
                    case AuthError.Failure: break;
                    case AuthError.InvalidCustomToken: break;
                    case AuthError.CustomTokenMismatch: break;
                    case AuthError.InvalidCredential: break;
                    case AuthError.UserDisabled: break;
                    case AuthError.AccountExistsWithDifferentCredentials: break;
                    case AuthError.OperationNotAllowed: break;
                    case AuthError.EmailAlreadyInUse: break;
                    case AuthError.RequiresRecentLogin: break;
                    case AuthError.CredentialAlreadyInUse: break;
                    case AuthError.InvalidEmail: break;
                    case AuthError.WrongPassword: break;
                    case AuthError.TooManyRequests: break;
                    case AuthError.UserNotFound: break;
                    case AuthError.ProviderAlreadyLinked: break;
                    case AuthError.NoSuchProvider: break;
                    case AuthError.InvalidUserToken: break;
                    case AuthError.UserTokenExpired: break;
                    case AuthError.NetworkRequestFailed: break;
                    case AuthError.InvalidApiKey: break;
                    case AuthError.AppNotAuthorized: break;
                    case AuthError.UserMismatch: break;
                    case AuthError.WeakPassword: break;
                    case AuthError.NoSignedInUser: break;
                    case AuthError.ApiNotAvailable: break;
                    case AuthError.ExpiredActionCode: break;
                    case AuthError.InvalidActionCode: break;
                    case AuthError.InvalidMessagePayload: break;
                    case AuthError.InvalidPhoneNumber: break;
                    case AuthError.MissingPhoneNumber: break;
                    case AuthError.InvalidRecipientEmail: break;
                    case AuthError.InvalidSender: break;
                    case AuthError.InvalidVerificationCode: break;
                    case AuthError.InvalidVerificationId: break;
                    case AuthError.MissingVerificationCode: break;
                    case AuthError.MissingVerificationId: break;
                    case AuthError.MissingEmail: break;
                    case AuthError.MissingPassword: break;
                    case AuthError.QuotaExceeded: break;
                    case AuthError.RetryPhoneAuth: break;
                    case AuthError.SessionExpired: break;
                    case AuthError.AppNotVerified: break;
                    case AuthError.AppVerificationFailed: break;
                    case AuthError.CaptchaCheckFailed: break;
                    case AuthError.InvalidAppCredential: break;
                    case AuthError.MissingAppCredential: break;
                    case AuthError.InvalidClientId: break;
                    case AuthError.InvalidContinueUri: break;
                    case AuthError.MissingContinueUri: break;
                    case AuthError.KeychainError: break;
                    case AuthError.MissingAppToken: break;
                    case AuthError.MissingIosBundleId: break;
                    case AuthError.NotificationNotForwarded: break;
                    case AuthError.UnauthorizedDomain: break;
                    case AuthError.WebContextAlreadyPresented: break;
                    case AuthError.WebContextCancelled: break;
                    case AuthError.DynamicLinkNotActivated: break;
                    case AuthError.Cancelled: break;
                    case AuthError.InvalidProviderId: break;
                    case AuthError.WebInternalError: break;
                    case AuthError.WebStorateUnsupported: break;
                    case AuthError.TenantIdMismatch: break;
                    case AuthError.UnsupportedTenantOperation: break;
                    case AuthError.InvalidLinkDomain: break;
                    case AuthError.RejectedCredential: break;
                    case AuthError.PhoneNumberNotFound: break;
                    case AuthError.InvalidTenantId: break;
                    case AuthError.MissingClientIdentifier: break;
                    case AuthError.MissingMultiFactorSession: break;
                    case AuthError.MissingMultiFactorInfo: break;
                    case AuthError.InvalidMultiFactorSession: break;
                    case AuthError.MultiFactorInfoNotFound: break;
                    case AuthError.AdminRestrictedOperation: break;
                    case AuthError.UnverifiedEmail: break;
                    case AuthError.SecondFactorAlreadyEnrolled: break;
                    case AuthError.MaximumSecondFactorCountExceeded: break;
                    case AuthError.UnsupportedFirstFactor: break;
                    case AuthError.EmailChangeNeedsVerification: break;
                    default: break;
                }
            }
            else
            {
                print ("Email sent successfully");
            }
        }
    }

    //Email verification notification panel
    public void closeNotificationPanel()
    {
        emailVerificationPanel.SetActive(false);
    }

    private void openNotificationPanel()
    {
        emailVerificationPanel.SetActive(true);
        showLogMsg("Please verify your email before logging in. A verification email has been sent to your email address.");
    }

    //Login panel
    public void openLoginPanel()
    {
        signUpUI.SetActive(false);
        loginUI.SetActive(true);
    }
    #endregion

    #region log in
    // Log in
    public void LogIn()
    {
        if (!checkPasswordMatch()) return;

        //loadingScreen.SetActive(true);

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        string email = LoginEmail.text;
        string password = LoginPassword.text;

        Credential credential = EmailAuthProvider.GetCredential(email, password);
        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("LogIn was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("LogIn encountered an error: " + task.Exception);
                return;
            }

            //loadingScreen.SetActive(false);
            AuthResult result = task.Result;
            Debug.LogFormat("User logged in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            if (result.User.IsEmailVerified)
            {
                SaveRememberMe();
                emailSuggestion.SaveLogin(LoginEmail.text , LoginPassword.text);

                showLogMsg("Login successful!");
                SceneController.Instance.NewTransitionPlan()
                                      .Load(new ParameterScene { Name = SceneDatabase.BOOTSTRAPONLINE })
                                      .WithFadeIn()
                                      .Perform().Forget();
                loginUI.SetActive(false);
            }
            else
            {
                showLogMsg("please verify email");
            }
        });
    }

    public bool checkPasswordMatch()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail.text))
        {
            openEmailPasswordNotificationPanel("Please enter your email.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(LoginPassword.text))
        {
            openEmailPasswordNotificationPanel("Please enter your password.");
            return false;
        }

        return true;
    }

     // Display a message to the user in the UI log text
    private void showLogMsg(string message)
    {
        if (logTxt != null && wrongEmailPasswordText != null)
        {
            logTxt.text = message;
            wrongEmailPasswordText.text = message;
        }
        else
        {
            Debug.Log(message);
        }
    }

    //wrong email or password notification panel
    public void closeEmailPasswordNotificationPanel()
    {
        emailPasswordNotificationPanel.SetActive(false);
    }

    private void openEmailPasswordNotificationPanel(string message)
    {
        emailPasswordNotificationPanel.SetActive(true);
        showLogMsg(message);
    }

    //Sign up panel
    public void openSignUpPanel()
    {
        signUpUI.SetActive(true);
        loginUI.SetActive(false);
    }
    #endregion

    #region forgot password
    public void ResetPassword()
    {
        if (!CheckForgotPasswordEmail())
            return;

        string email = forgotPasswordEmail.text.Trim();

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("Reset Password was canceled.");
                showLogMsg("Password reset was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Reset Password Error: " + task.Exception);

                FirebaseException firebaseException =
                    task.Exception.GetBaseException() as FirebaseException;

                if (firebaseException != null)
                {
                    AuthError error = (AuthError)firebaseException.ErrorCode;

                    switch (error)
                    {
                        case AuthError.InvalidEmail:
                            showLogMsg("Invalid email address.");
                            break;

                        case AuthError.UserNotFound:
                            showLogMsg("No account found with this email.");
                            break;

                        case AuthError.NetworkRequestFailed:
                            showLogMsg("Network error. Please try again.");
                            break;

                        default:
                            showLogMsg("Failed to send password reset email.");
                            break;
                    }
                }
                else
                {
                    showLogMsg("Failed to send password reset email.");
                }

                return;
            }

            showLogMsg("Password reset email has been sent.");

            forgotPasswordPanel.SetActive(false);

            Debug.Log("Password reset email sent successfully.");
        });
    }

    private bool CheckForgotPasswordEmail()
    {
        if (string.IsNullOrWhiteSpace(forgotPasswordEmail.text))
        {
            showLogMsg("Please enter your email.");
            return false;
        }

        if (!Regex.IsMatch(
            forgotPasswordEmail.text.Trim(),
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            showLogMsg("Invalid email format.");
            return false;
        }

        return true;
    }

    public void OpenForgotPasswordPanel()
    {
        forgotPasswordEmail.text = LoginEmail.text;
        loginUI.SetActive(false);
        forgotPasswordPanel.SetActive(true);
    }

    public void CloseForgotPasswordPanel()
    {
        forgotPasswordPanel.SetActive(false);
        loginUI.SetActive(true);
    }

    #endregion

    #region remember me functionality
    // Load the remembered email from PlayerPrefs and set it in the login email input field
    public void ToggleRememberMe()
    {
        PlayerPrefs.SetInt(
            "RememberMe",
            rememberMeToggle.isOn ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    private void LoadRememberedEmail()
    {
        bool remember = PlayerPrefs.GetInt("RememberMe", 0) == 1;

        rememberMeToggle.isOn = remember;
    }

    private void SaveRememberMe()
    {
        if (rememberMeToggle.isOn)
        {
            PlayerPrefs.SetInt("RememberMe", 1);
            PlayerPrefs.SetString("SavedEmail", LoginEmail.text);
            PlayerPrefs.SetString("SavedPassword", LoginPassword.text);      
        }
        else
        {
            PlayerPrefs.SetInt("RememberMe", 0);
            PlayerPrefs.DeleteKey("SavedEmail");
            PlayerPrefs.DeleteKey("SavedPassword");
        }

        PlayerPrefs.Save();
    }
    #endregion 
}