/*  =====================================================================
    LoginManager.cs  –  Google Sign-In for AR Anatomy
    =====================================================================

    HOW TO ENABLE REAL GOOGLE LOGIN (Android):
    ------------------------------------------
    1. Create a Firebase project at https://console.firebase.google.com
       • Add an Android app (use your Unity Bundle ID, e.g. com.YourCompany.ARAnatomy)
       • Download google-services.json → place it in Assets/
       • Enable "Google" under Authentication → Sign-in method

    2. Download the Firebase Unity SDK (v12+):
       https://firebase.google.com/docs/unity/setup
       • Import FirebaseAuth.unitypackage into this project

    3. Download the Google Sign-In Plugin for Unity:
       https://github.com/googlesamples/google-signin-unity/releases
       • Import GoogleSignIn.unitypackage into this project

    4. In Unity → Edit → Project Settings → Player → Other Settings:
       • Add   FIREBASE_ENABLED   to Scripting Define Symbols

    5. Paste your Web Client ID (OAuth 2.0 Client ID – type "Web application")
       from Firebase Console → Project Settings → General → Your apps → Web API Key
       into the  webClientId  field on the LoginManager component in the Inspector.

    Without the above steps, the app runs in DEMO mode (mock login).
    ===================================================================== */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if FIREBASE_ENABLED
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
#endif

public class LoginManager : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────
    [Header("Buttons")]
    public Button googleSignInButton;
    public Button guestButton;

    [Header("Status / Loading")]
    public TextMeshProUGUI statusText;
    public GameObject      loadingSpinner;

    [Header("Scene Navigation")]
    [Tooltip("Scene to load after a successful login.")]
    public string homeSceneName = "Start-Scene";

    [Header("Azure Functions URL  (Agent 1, 2 & 3)")]
    [Tooltip("Base URL of your Azure Function App, e.g. https://your-app.azurewebsites.net  (no trailing slash)")]
    public string azureFunctionsBaseUrl = "https://neuroai-backend-production.up.railway.app";

    [Header("Firebase Config  (set before using real login)")]
    [Tooltip("Web Client ID from Firebase Console → Project Settings → Your apps")]
    public string webClientId = "YOUR_WEB_CLIENT_ID_HERE";

    [Header("Temporary Feature Toggles")]
    [Tooltip("Temporarily disable Google Sign-In without deleting integration code.")]
    public bool disableGoogleLogin = true;

    // ── Private state ─────────────────────────────────────────────────
#if FIREBASE_ENABLED
    private FirebaseAuth auth;
    private bool         firebaseReady = false;
#endif

    // ── Unity lifecycle ───────────────────────────────────────────────
    private void Awake()
    {
        // Ensure UserSession exists before anything else
        if (UserSession.Instance == null)
        {
            var go = new GameObject("UserSession");
            go.AddComponent<UserSession>();
        }

        SetLoading(false);
    }

    private void Start()
    {
        // Already logged in from a previous session → load profile then go home
        if (UserSession.Instance.IsLoggedIn)
        {
            // Still need to load the backend profile so Agent 1 data is available
            LoadProfileThenNavigate();
            return;
        }

        // Wire up buttons
        if (googleSignInButton != null)
        {
            if (disableGoogleLogin)
            {
                googleSignInButton.interactable = false;
            }
            else
            {
                googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
            }
        }
        else
            Debug.LogWarning("[LoginManager] Google Sign-In button not assigned.");

        if (guestButton != null)
            guestButton.onClick.AddListener(OnGuestClicked);

#if FIREBASE_ENABLED
        if (disableGoogleLogin)
        {
            SetStatus("Google login is temporarily disabled. Use Guest login.");
        }
        else
        {
            SetStatus("Initializing…");
            InitializeFirebase();
        }
#else
        if (disableGoogleLogin)
            SetStatus("Google login is temporarily disabled. Use Guest login.");
        else
            SetStatus("Demo mode – tap Sign In to try the app");
#endif
    }

    // ── Button handlers ───────────────────────────────────────────────

    private void OnGuestClicked()
    {
        UserSession.Instance.SetGuest();
        LoadProfileThenNavigate();
    }

#if FIREBASE_ENABLED

    // ── Firebase + Google Sign-In (production) ────────────────────────

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                GoogleSignIn.Configuration = new GoogleSignInConfiguration
                {
                    WebClientId    = webClientId,
                    RequestIdToken = true,
                    UseGameSignIn  = false
                };

                firebaseReady = true;
                SetStatus("Ready – sign in to continue");
            }
            else
            {
                SetStatus($"Firebase error: {task.Result}");
                Debug.LogError($"[LoginManager] Firebase dependency error: {task.Result}");
            }
        });
    }

#if UNITY_EDITOR
    private void OnGoogleSignInClicked()
#else
    private async void OnGoogleSignInClicked()
#endif
    {
        if (!firebaseReady)
        {
            SetStatus("Still initializing, please wait…");
            return;
        }

#if UNITY_EDITOR
        // Google Sign-In requires Android JNI — it cannot run in the Unity Editor.
        // Fall back to demo credentials so you can still test the full app flow.
        Debug.LogWarning("[LoginManager] Google Sign-In is not supported in the Unity Editor. Using demo credentials.");
        UserSession.Instance.SetUser("demo-user-editor", "Editor Student", "editor@demo.com", string.Empty);
        SetLoading(false);
        LoadProfileThenNavigate();
#else
        SetLoading(true);
        SetStatus("Opening Google sign-in…");

        try
        {
            // 1. Google Sign-In
            GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();

            // 2. Exchange Google token for Firebase credential
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            // 3. Sign in to Firebase
            // SignInWithCredentialAsync returns FirebaseUser in SDK <v9, AuthResult in v9+
            // We handle both by assigning to FirebaseUser directly.
            FirebaseUser fbUser = await auth.SignInWithCredentialAsync(credential);

            // 4. Persist in UserSession
            UserSession.Instance.SetUser(
                fbUser.UserId,
                fbUser.DisplayName,
                fbUser.Email,
                fbUser.PhotoUrl?.AbsoluteUri ?? string.Empty
            );

            SetStatus($"Welcome, {fbUser.DisplayName}!");
            SetLoading(false);
            LoadProfileThenNavigate();
        }
        catch (System.Exception ex)
        {
            SetLoading(false);
            SetStatus("Sign-in failed. Please try again.");
            Debug.LogError($"[LoginManager] Google Sign-In failed: {ex}");
        }
#endif
    }

#else

    // ── Demo / mock login (no Firebase) ──────────────────────────────

    private void OnGoogleSignInClicked()
    {
        SetLoading(true);
        SetStatus("Signing in (demo)…");

        // Simulate async delay then log in with mock data
        StartCoroutine(DemoLoginCoroutine());
    }

    private System.Collections.IEnumerator DemoLoginCoroutine()
    {
        yield return new UnityEngine.WaitForSeconds(1.2f);

        UserSession.Instance.SetUser(
            userId:      "demo-user-001",
            displayName: "Demo Student",
            email:       "student@demo.com",
            photoUrl:    string.Empty
        );

        SetLoading(false);
        LoadProfileThenNavigate();
    }

#endif

    // ── Agent 1: load profile then navigate ─────────────────────────

    private void LoadProfileThenNavigate()
    {
        // Ensure LearnerProfileManager exists (it persists via DontDestroyOnLoad)
        if (LearnerProfileManager.Instance == null)
        {
            var go = new UnityEngine.GameObject("LearnerProfileManager");
            var lpm = go.AddComponent<LearnerProfileManager>();
            // Pass the URL before any service call is made (lazy-init uses this value)
            lpm.azureFunctionsBaseUrl = azureFunctionsBaseUrl;
        }

        SetStatus("Loading your profile…");
        SetLoading(true);

        LearnerProfileManager.Instance.LoadProfileForCurrentUser(
            onDone: () =>
            {
                SetLoading(false);
                var profile = LearnerProfileManager.Instance.Profile;

                // New user — send to onboarding; returning user — go home
                if (profile != null && !profile.onboarding.completed)
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Onboarding-Scene");
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(homeSceneName);
            },
            onError: err =>
            {
                // Backend unreachable — still let user in with offline profile
                Debug.LogWarning($"[LoginManager] Profile load error (offline mode): {err}");
                SetLoading(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene(homeSceneName);
            }
        );
    }

    // ── UI helpers ────────────────────────────────────────────────────

    private void SetLoading(bool loading)
    {
        if (loadingSpinner     != null) loadingSpinner.SetActive(loading);
        if (googleSignInButton != null) googleSignInButton.interactable = !loading;
        if (guestButton        != null) guestButton.interactable        = !loading;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
