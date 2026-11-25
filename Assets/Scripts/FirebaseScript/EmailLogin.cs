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
using Firebase.Database;

public class EmailLogin : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private TMP_InputField LoginEmail;
    [SerializeField] private TMP_InputField LoginPassword;
    private bool isLoggingIn;
    [Header("Sign up")]
    [SerializeField] private TMP_InputField SignUpEmail;
    [SerializeField] private TMP_InputField SignUpPassword;
    [SerializeField] private TMP_InputField SignUpPasswordConfirm;

    [Header("Extra")]
    [SerializeField] private Toggle rememberMeToggle;

    [Header("UI")]
    [SerializeField] private GameObject loginRootPanel;
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

    public static int LastEscConsumedFrame { get; private set; } = -1;

    private RectTransform notificationPanelRect;
    private Camera uiCamera;
    private int panelOpenedFrame = -1;

    private void Awake()
    {
        if (emailPasswordNotificationPanel != null)
        {
            notificationPanelRect = emailPasswordNotificationPanel.GetComponent<RectTransform>();
            Canvas parentCanvas = emailPasswordNotificationPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = parentCanvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        ResetLoginFlowState();
    }

    private void Start()
    {
        LoadRememberedEmail();
        if (emailSuggestionPanel != null)
            emailSuggestionPanel.SetActive(false);
    }

    private void Update()
    {
        if (emailPasswordNotificationPanel != null
            && emailPasswordNotificationPanel.activeSelf
            && Time.frameCount > panelOpenedFrame
            && Input.GetMouseButtonDown(0))
        {
            if (notificationPanelRect != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(notificationPanelRect, Input.mousePosition, uiCamera))
            {
                closeEmailPasswordNotificationPanel();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeBack();
        }
    }

    private void HandleEscapeBack()
    {
        LastEscConsumedFrame = Time.frameCount;

        if (emailPasswordNotificationPanel != null && emailPasswordNotificationPanel.activeSelf)
        {
            closeEmailPasswordNotificationPanel();
            return;
        }

        if (emailVerificationPanel != null && emailVerificationPanel.activeSelf)
        {
            closeNotificationPanel();
            return;
        }

        if (emailSuggestionPanel != null && emailSuggestionPanel.activeSelf)
        {
            emailSuggestionPanel.SetActive(false);
            return;
        }

        if (forgotPasswordPanel != null && forgotPasswordPanel.activeSelf)
        {
            CloseForgotPasswordPanel();
            return;
        }

        if (signUpUI != null && signUpUI.activeSelf)
        {
            openLoginPanel();
            return;
        }

        CloseEntireLoginPanel();
    }

    public void ResetLoginFlowState()
    {
        if (loginUI != null) loginUI.SetActive(true);
        if (signUpUI != null) signUpUI.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (emailPasswordNotificationPanel != null) emailPasswordNotificationPanel.SetActive(false);
        if (emailVerificationPanel != null) emailVerificationPanel.SetActive(false);
        if (emailSuggestionPanel != null) emailSuggestionPanel.SetActive(false);

        if (SignUpEmail != null) SignUpEmail.text = "";
        if (SignUpPassword != null) SignUpPassword.text = "";
        if (SignUpPasswordConfirm != null) SignUpPasswordConfirm.text = "";
        if (forgotPasswordEmail != null) forgotPasswordEmail.text = "";

        LoadRememberedEmail();
        if (logTxt != null) logTxt.text = "";
        if (wrongEmailPasswordText != null) wrongEmailPasswordText.text = "";
    }

    public void CloseEntireLoginPanel()
    {
        ResetLoginFlowState();
        GameObject target = loginRootPanel != null ? loginRootPanel : gameObject;
        if (UIHistoryManager.Instance != null)
            UIHistoryManager.Instance.ClosePanel(target);
        else
            target.SetActive(false);
    }

    #region sign up
    private bool isSigningUp;

    public void SignUp()
    {
        if (isSigningUp) return;
        if (!CheckSignUpValidation()) return;

        isSigningUp = true;
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        string email = SignUpEmail.text.Trim();
        string password = SignUpPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            try
            {
                if (task.IsCanceled)
                {
                    openEmailPasswordNotificationPanel("Sign up was canceled.");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError("SignUp encountered an error: " + task.Exception);
                    FirebaseException firebaseException = task.Exception?.GetBaseException() as FirebaseException;

                    if (firebaseException != null)
                    {
                        AuthError error = (AuthError)firebaseException.ErrorCode;
                        switch (error)
                        {
                            case AuthError.EmailAlreadyInUse:
                                openEmailPasswordNotificationPanel("This email is already registered.");
                                break;
                            case AuthError.InvalidEmail:
                                openEmailPasswordNotificationPanel("Invalid email format.");
                                break;
                            case AuthError.WeakPassword:
                                openEmailPasswordNotificationPanel("Password must be at least 6 characters.");
                                break;
                            case AuthError.NetworkRequestFailed:
                                openEmailPasswordNotificationPanel("Network error. Please try again.");
                                break;
                            default:
                                openEmailPasswordNotificationPanel("Sign up failed. Please check your information.");
                                break;
                        }
                    }
                    else
                    {
                        openEmailPasswordNotificationPanel("Sign up failed.");
                    }
                    return;
                }

                AuthResult result = task.Result;
                FirebaseUser newUser = result.User;
                Debug.LogFormat("User signed up successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

                if (SignUpEmail != null) SignUpEmail.text = "";
                if (SignUpPassword != null) SignUpPassword.text = "";
                if (SignUpPasswordConfirm != null) SignUpPasswordConfirm.text = "";

                // Chỉ chuyển về loginUI sau khi tạo tài khoản thành công
                if (signUpUI != null) signUpUI.SetActive(false);
                if (loginUI != null) loginUI.SetActive(true);

                if (newUser.IsEmailVerified)
                {
                    openEmailPasswordNotificationPanel("Sign up successful! Please log in.");
                }
                else
                {
                    openNotificationPanel();
                    SendEmailVerificationAndSignOutAsync(newUser).Forget();
                }
            }
            finally
            {
                isSigningUp = false;
            }
        });
    }

    private bool CheckSignUpValidation()
    {
        if (SignUpEmail == null || string.IsNullOrWhiteSpace(SignUpEmail.text))
        {
            openEmailPasswordNotificationPanel("Please enter your email.");
            return false;
        }

        string email = SignUpEmail.text.Trim();
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            openEmailPasswordNotificationPanel("Invalid email format.");
            return false;
        }

        if (SignUpPassword == null || string.IsNullOrEmpty(SignUpPassword.text))
        {
            openEmailPasswordNotificationPanel("Please enter your password.");
            return false;
        }

        if (SignUpPassword.text.Length < 6)
        {
            openEmailPasswordNotificationPanel("Password must be at least 6 characters.");
            return false;
        }

        if (SignUpPasswordConfirm != null && SignUpPassword.text != SignUpPasswordConfirm.text)
        {
            openEmailPasswordNotificationPanel("Passwords do not match.");
            return false;
        }

        return true;
    }

    public void SendEmailVerification()
    {
        SendEmailVerificationAndSignOutAsync(FirebaseAuth.DefaultInstance.CurrentUser).Forget();
    }

    private async UniTaskVoid SendEmailVerificationAndSignOutAsync(FirebaseUser targetUser)
    {
        try
        {
            if (targetUser != null)
            {
                await targetUser.SendEmailVerificationAsync();
                Debug.Log("Verification email sent successfully.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Email verification send error: " + ex.Message);
        }
        finally
        {
            // Đăng xuất phiên tạm sau khi gửi xong link xác thực để tránh lưu token chưa kích hoạt trên máy
            if (FirebaseAuth.DefaultInstance.CurrentUser != null &&
                targetUser != null &&
                FirebaseAuth.DefaultInstance.CurrentUser.UserId == targetUser.UserId)
            {
                FirebaseAuth.DefaultInstance.SignOut();
            }
        }
    }

    public void closeNotificationPanel()
    {
        if (emailVerificationPanel != null)
            emailVerificationPanel.SetActive(false);
    }

    private void openNotificationPanel()
    {
        if (emailVerificationPanel != null)
            emailVerificationPanel.SetActive(false);

        openEmailPasswordNotificationPanel(
            "A verification link has been sent to your email. Please check your inbox (or Spam) and verify before logging in."
        );
    }

    public void openLoginPanel()
    {
        if (signUpUI != null) signUpUI.SetActive(false);
        if (loginUI != null) loginUI.SetActive(true);
    }
    #endregion

    #region log in
    public void LogIn()
    {
        if (isLoggingIn) return;
        if (!checkValidation()) return;

        isLoggingIn = true;

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        string email = LoginEmail.text;
        string password = LoginPassword.text;

        Credential credential = EmailAuthProvider.GetCredential(email, password);
        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(async task =>
        {
            try
            {
                if (task.IsCanceled)
                {
                    Debug.Log("LogIn was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.Log("LogIn encountered an error: " + task.Exception);

                    FirebaseException firebaseException =
                        task.Exception.GetBaseException() as FirebaseException;

                    if (firebaseException != null)
                    {
                        AuthError error = (AuthError)firebaseException.ErrorCode;

                        switch (error)
                        {
                            case AuthError.WrongPassword:
                                openEmailPasswordNotificationPanel("Incorrect password.");
                                break;

                            case AuthError.InvalidEmail:
                                openEmailPasswordNotificationPanel("Invalid email.");
                                break;

                            case AuthError.UserNotFound:
                                openEmailPasswordNotificationPanel("Account does not exist.");
                                break;

                            case AuthError.InvalidCredential:
                                openEmailPasswordNotificationPanel("Incorrect email or password.");
                                break;

                            case AuthError.UserDisabled:
                                openEmailPasswordNotificationPanel("This account has been disabled.");
                                break;

                            case AuthError.NetworkRequestFailed:
                                openEmailPasswordNotificationPanel("Network error. Please try again.");
                                break;

                            default:
                                openEmailPasswordNotificationPanel("Login failed. Please check your email and password.");
                                break;
                        }
                    }
                    else
                    {
                        openEmailPasswordNotificationPanel("Login failed.");
                    }

                    return;
                }

                AuthResult result = task.Result;
                string userId = result.User.UserId;
                Debug.LogFormat("User logged in successfully: {0} ({1})",
                    result.User.DisplayName, userId);

                if (result.User.IsEmailVerified)
                {
                    // Khóa cố định UserID cho cửa sổ này ngay lập tức để tránh cửa sổ ParrelSync khác ghi đè
                    FirebaseManager.SetUserID(userId);

                    string mySessionId = System.Guid.NewGuid().ToString("N");
                    bool blockedByOtherDevice = false;

                    try
                    {
                        FirebaseDatabase.DefaultInstance.GoOnline();

                        var userRef = FirebaseDatabase.DefaultInstance.GetReference($"Users/{userId}");
                        var sessionRef = userRef.Child("Session");

                        var pingRef = userRef.Child($"SessionPing/{mySessionId}");
                        await pingRef.SetValueAsync(ServerValue.Timestamp);
                        var pingSnap = await pingRef.GetValueAsync();
                        long serverNow = (pingSnap.Exists && pingSnap.Value != null)
                            ? System.Convert.ToInt64(pingSnap.Value)
                            : System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        _ = pingRef.RemoveValueAsync();

                        var currentSessionSnap = await sessionRef.GetValueAsync();
                        if (currentSessionSnap.Exists && currentSessionSnap.HasChild("ActiveSessionId"))
                        {
                            string existingId = currentSessionSnap.Child("ActiveSessionId").Value?.ToString();
                            long lastActive = 0;
                            if (currentSessionSnap.HasChild("LastActive") && currentSessionSnap.Child("LastActive").Value != null)
                            {
                                System.Int64.TryParse(currentSessionSnap.Child("LastActive").Value.ToString(), out lastActive);
                            }

                            if (!string.IsNullOrEmpty(existingId)
                                && existingId != mySessionId
                                && lastActive > 0
                                && (serverNow - lastActive) < 30000)
                            {
                                FirebaseManager.ClearUserID();
                                auth.SignOut();
                                openEmailPasswordNotificationPanel("This account is already logged in on another device.");
                                return;
                            }
                        }

                        await sessionRef.RunTransaction(mutableData =>
                        {
                            blockedByOtherDevice = false;

                            if (mutableData.Value != null && mutableData.HasChild("ActiveSessionId"))
                            {
                                string existingId = mutableData.Child("ActiveSessionId").Value?.ToString();
                                long lastActive = 0;
                                if (mutableData.HasChild("LastActive") && mutableData.Child("LastActive").Value != null)
                                {
                                    System.Int64.TryParse(mutableData.Child("LastActive").Value.ToString(), out lastActive);
                                }

                                if (!string.IsNullOrEmpty(existingId)
                                    && existingId != mySessionId
                                    && lastActive > 0
                                    && (serverNow - lastActive) < 30000)
                                {
                                    blockedByOtherDevice = true;
                                    return TransactionResult.Abort();
                                }
                            }

                            mutableData.Value = new Dictionary<string, object>
                            {
                                ["ActiveSessionId"] = mySessionId,
                                ["LastActive"] = ServerValue.Timestamp,
                                ["DeviceName"] = SystemInfo.deviceName
                            };
                            return TransactionResult.Success(mutableData);
                        });

                        if (blockedByOtherDevice)
                        {
                            FirebaseManager.ClearUserID();
                            auth.SignOut();
                            openEmailPasswordNotificationPanel("This account is already logged in on another device.");
                            return;
                        }

                        await sessionRef.OnDisconnect().RemoveValue();
                        await userRef.Child("Presence/Status").OnDisconnect().SetValue((int)OnlineStatus.Offline);

                        AccountSessionGuard.StartSession(userId, mySessionId);
                    }
                    catch (System.Exception ex)
                    {
                        FirebaseManager.ClearUserID();
                        auth.SignOut();
                        if (blockedByOtherDevice)
                        {
                            openEmailPasswordNotificationPanel("This account is already logged in on another device.");
                        }
                        else
                        {
                            Debug.LogWarning("Không thể xác thực phiên đăng nhập: " + ex.Message);
                            openEmailPasswordNotificationPanel("Unable to verify session. Please check your connection and try again.");
                        }
                        return;
                    }

                    SaveRememberMe();
                    if (emailSuggestion != null)
                        emailSuggestion.SaveLogin(LoginEmail.text, LoginPassword.text);

                    showLogMsg("Login successful!");
                    SceneController.Instance.NewTransitionPlan()
                                          .Load(new ParameterScene { Name = SceneDatabase.BOOTSTRAPONLINE })
                                          .WithFadeIn()
                                          .Perform().Forget();
                    loginUI.SetActive(false);
                }
                else
                {
                    auth.SignOut();
                    openEmailPasswordNotificationPanel("Your email is not verified yet. Please click the verification link sent to your email before logging in.");
                }
            }
            finally
            {
                isLoggingIn = false;
            }
        });
    }

    public bool checkValidation()
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

    private void showLogMsg(string message)
    {
        if (logTxt != null)
            logTxt.text = message;

        if (wrongEmailPasswordText != null)
            wrongEmailPasswordText.text = message;

        if (logTxt == null && wrongEmailPasswordText == null)
            Debug.Log(message);
    }
    public void closeEmailPasswordNotificationPanel()
    {
        emailPasswordNotificationPanel.SetActive(false);
    }

    private void openEmailPasswordNotificationPanel(string message)
    {
        panelOpenedFrame = Time.frameCount;
        emailPasswordNotificationPanel.SetActive(true);
        showLogMsg(message);
    }

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
                            openEmailPasswordNotificationPanel("Invalid email address.");
                            break;

                        case AuthError.UserNotFound:
                            openEmailPasswordNotificationPanel("No account found with this email.");
                            break;

                        case AuthError.NetworkRequestFailed:
                            openEmailPasswordNotificationPanel("Network error. Please try again.");
                            break;

                        default:
                            openEmailPasswordNotificationPanel("Failed to send password reset email.");
                            break;
                    }
                }
                else
                {
                    openEmailPasswordNotificationPanel("Failed to send password reset email.");
                }

                return;
            }

            openEmailPasswordNotificationPanel("If the email is registered, you will receive a password reset email shortly.");
            CloseForgotPasswordPanel();
        });
    }

    private bool CheckForgotPasswordEmail()
    {
        if (string.IsNullOrWhiteSpace(forgotPasswordEmail.text))
        {
            openEmailPasswordNotificationPanel("Please enter your email.");
            return false;
        }

        if (!Regex.IsMatch(
            forgotPasswordEmail.text.Trim(),
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            openEmailPasswordNotificationPanel("Invalid email format.");
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
    public void ToggleRememberMe()
    {
        if (rememberMeToggle == null) return;

        bool isRemember = rememberMeToggle.isOn;
        PlayerPrefs.SetInt("RememberMe", isRemember ? 1 : 0);

        if (!isRemember)
        {
            PlayerPrefs.DeleteKey("SavedEmail");
            PlayerPrefs.DeleteKey("SavedPassword");
        }
        else if (LoginEmail != null && LoginPassword != null)
        {
            PlayerPrefs.SetString("SavedEmail", LoginEmail.text.Trim());
            PlayerPrefs.SetString("SavedPassword", LoginPassword.text);
        }

        PlayerPrefs.Save();
    }

    private void LoadRememberedEmail()
    {
        bool remember = PlayerPrefs.GetInt("RememberMe", 0) == 1;

        if (rememberMeToggle != null)
            rememberMeToggle.SetIsOnWithoutNotify(remember);

        if (remember)
        {
            if (LoginEmail != null)
                LoginEmail.text = PlayerPrefs.GetString("SavedEmail", "");
            if (LoginPassword != null)
                LoginPassword.text = PlayerPrefs.GetString("SavedPassword", "");
        }
        else
        {
            if (LoginEmail != null) LoginEmail.text = "";
            if (LoginPassword != null) LoginPassword.text = "";
        }
    }

    private void SaveRememberMe()
    {
        if (rememberMeToggle != null && rememberMeToggle.isOn)
        {
            PlayerPrefs.SetInt("RememberMe", 1);
            PlayerPrefs.SetString("SavedEmail", LoginEmail.text.Trim());
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

public class AccountSessionGuard : MonoBehaviour
{
    private static AccountSessionGuard _instance;

    private string _userId;
    private string _sessionId;
    private DatabaseReference _lastActiveRef;
    private float _heartbeatTimer;
    private bool _isActive;

    public static void StartSession(string userId, string sessionId)
    {
        if (_instance == null)
        {
            var go = new GameObject("AccountSessionGuard");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AccountSessionGuard>();
        }

        _instance.Bind(userId, sessionId);
    }

    public static void ClearLocalSession()
    {
        if (_instance != null)
            _instance.ReleaseSession(clearOnServer: true);
    }

    private void Bind(string userId, string sessionId)
    {
        _userId = userId;
        _sessionId = sessionId;
        _heartbeatTimer = 0f;
        _isActive = true;

        _lastActiveRef = FirebaseDatabase.DefaultInstance.GetReference($"Users/{_userId}/Session/LastActive");
        _lastActiveRef.SetValueAsync(ServerValue.Timestamp);
    }

    private void Update()
    {
        if (!_isActive) return;

        _heartbeatTimer += Time.unscaledDeltaTime;
        if (_heartbeatTimer >= 4f)
        {
            _heartbeatTimer = 0f;
            if (_lastActiveRef != null)
            {
                _lastActiveRef.SetValueAsync(ServerValue.Timestamp);
            }
        }
    }

    private void ReleaseSession(bool clearOnServer)
    {
        if (!_isActive) return;
        _isActive = false;

        if (clearOnServer && !string.IsNullOrEmpty(_userId))
        {
            string targetUid = _userId;
            string expectedSession = _sessionId;
            var userRef = FirebaseDatabase.DefaultInstance.GetReference($"Users/{targetUid}");

            userRef.Child("Session").RunTransaction(mutableData =>
            {
                if (mutableData.Value != null && mutableData.HasChild("ActiveSessionId"))
                {
                    string serverSession = mutableData.Child("ActiveSessionId").Value?.ToString();
                    if (serverSession == expectedSession)
                    {
                        mutableData.Value = null;
                        return TransactionResult.Success(mutableData);
                    }
                }
                return TransactionResult.Abort();
            });

            userRef.Child("Presence/Status").SetValueAsync((int)OnlineStatus.Offline);
        }
    }

    private void OnApplicationQuit()
    {
        ReleaseSession(clearOnServer: true);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}