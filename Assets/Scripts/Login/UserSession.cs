using UnityEngine;

/// <summary>
/// Persistent singleton that holds the currently logged-in student's data.
/// Survives scene loads via DontDestroyOnLoad.
/// Session is also persisted to PlayerPrefs so the user stays logged in after
/// closing and re-opening the app.
/// Access anywhere with UserSession.Instance
/// </summary>
public class UserSession : MonoBehaviour
{
    public static UserSession Instance { get; private set; }

    // ── User properties ─────────────────────────────
    public string UserId       { get; private set; }
    public string DisplayName  { get; private set; }
    public string Email        { get; private set; }
    public string PhotoUrl     { get; private set; }
    public bool   IsLoggedIn   { get; private set; }
    public bool   IsGuest      { get; private set; }

    // PlayerPrefs keys
    private const string KEY_USER_ID      = "session_userId";
    private const string KEY_DISPLAY_NAME = "session_displayName";
    private const string KEY_EMAIL        = "session_email";
    private const string KEY_PHOTO_URL    = "session_photoUrl";
    private const string KEY_IS_GUEST     = "session_isGuest";

    // ── Lifecycle ────────────────────────────────────
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryRestoreSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Restore from PlayerPrefs ─────────────────────
    private void TryRestoreSession()
    {
        string savedId = PlayerPrefs.GetString(KEY_USER_ID, string.Empty);
        if (string.IsNullOrEmpty(savedId)) return;

        UserId      = savedId;
        DisplayName = PlayerPrefs.GetString(KEY_DISPLAY_NAME, string.Empty);
        Email       = PlayerPrefs.GetString(KEY_EMAIL, string.Empty);
        PhotoUrl    = PlayerPrefs.GetString(KEY_PHOTO_URL, string.Empty);
        IsGuest     = PlayerPrefs.GetInt(KEY_IS_GUEST, 0) == 1;
        IsLoggedIn  = true;
        Debug.Log($"[UserSession] Restored session: {DisplayName} (guest={IsGuest})");
    }

    // ── Public API ───────────────────────────────────

    /// <summary>Called by LoginManager after a successful Google sign-in.</summary>
    public void SetUser(string userId, string displayName, string email, string photoUrl)
    {
        UserId      = userId;
        DisplayName = displayName;
        Email       = email;
        PhotoUrl    = photoUrl;
        IsLoggedIn  = true;
        IsGuest     = false;

        PlayerPrefs.SetString(KEY_USER_ID,      userId);
        PlayerPrefs.SetString(KEY_DISPLAY_NAME, displayName);
        PlayerPrefs.SetString(KEY_EMAIL,        email);
        PlayerPrefs.SetString(KEY_PHOTO_URL,    photoUrl);
        PlayerPrefs.SetInt(KEY_IS_GUEST, 0);
        PlayerPrefs.Save();

        Debug.Log($"[UserSession] Logged in: {displayName} ({email})");
    }

    /// <summary>Called when the user continues as a guest.</summary>
    public void SetGuest()
    {
        UserId      = "guest-" + SystemInfo.deviceUniqueIdentifier;
        DisplayName = "Guest";
        Email       = string.Empty;
        PhotoUrl    = string.Empty;
        IsLoggedIn  = true;
        IsGuest     = true;

        PlayerPrefs.SetString(KEY_USER_ID,      UserId);
        PlayerPrefs.SetString(KEY_DISPLAY_NAME, "Guest");
        PlayerPrefs.SetString(KEY_EMAIL,        string.Empty);
        PlayerPrefs.SetString(KEY_PHOTO_URL,    string.Empty);
        PlayerPrefs.SetInt(KEY_IS_GUEST, 1);
        PlayerPrefs.Save();

        Debug.Log("[UserSession] Continuing as Guest.");
    }

    /// <summary>Signs the user out and clears all stored data.</summary>
    public void SignOut()
    {
        UserId = DisplayName = Email = PhotoUrl = null;
        IsLoggedIn = false;
        IsGuest    = false;

        PlayerPrefs.DeleteKey(KEY_USER_ID);
        PlayerPrefs.DeleteKey(KEY_DISPLAY_NAME);
        PlayerPrefs.DeleteKey(KEY_EMAIL);
        PlayerPrefs.DeleteKey(KEY_PHOTO_URL);
        PlayerPrefs.DeleteKey(KEY_IS_GUEST);
        PlayerPrefs.Save();

        Debug.Log("[UserSession] Signed out.");
    }
}
