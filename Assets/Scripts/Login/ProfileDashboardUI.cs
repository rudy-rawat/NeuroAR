using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Controls the slide-in profile dashboard panel in the Start-Scene (and any scene).
/// 
/// Displays the logged-in student's:
///   - Profile photo (downloaded from Google account URL)
///   - Display name
///   - Email
///   - User ID (used internally as the Agent 1 / Cosmos DB key)
///   - Guest badge if not signed in with Google
///
/// Also handles Sign Out → redirects to Login-Scene.
/// 
/// Wire up all fields in the Inspector, then call ToggleDashboard() from a button.
/// </summary>
public class ProfileDashboardUI : MonoBehaviour
{
    // ── Panel reference ───────────────────────────────────────────────
    [Header("Dashboard Panel")]
    [Tooltip("The root panel GameObject to show/hide.")]
    public GameObject dashboardPanel;

    // ── Profile fields ────────────────────────────────────────────────
    [Header("Profile Fields")]
    public RawImage    profilePhoto;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI userIdText;       // shows UID (Agent 1 Cosmos DB key)
    public GameObject  guestBadge;           // "Guest" label, shown when IsGuest = true
    public GameObject  googleBadge;          // "Google" label, shown when signed in

    // ── Buttons ───────────────────────────────────────────────────────
    [Header("Buttons")]
    [Tooltip("The avatar / profile icon button that opens the dashboard.")]
    public Button      profileButton;
    [Tooltip("Sign-out button inside the dashboard.")]
    public Button      signOutButton;
    [Tooltip("Close / back button inside the dashboard.")]
    public Button      closeButton;

    // ── Avatar shown on the HUD when dashboard is closed ─────────────
    [Header("HUD Avatar (small icon)")]
    public RawImage    hudAvatarImage;       // small circle avatar on the main screen

    // ── Agent 1 stat labels (wired by ProfileDashboardSetup editor script) ──
    [Header("Agent 1 Stats (auto-populated)")]
    public TextMeshProUGUI organsStudiedLabel;   // "Organs studied: 4"
    public TextMeshProUGUI quizScoreLabel;       // "Quiz score: 60%"
    public TextMeshProUGUI weakConceptsLabel;    // "Weak: heart valves, …"
    public TextMeshProUGUI recommendedLabel;     // "Next: Kidney"
    public TextMeshProUGUI levelLabel;           // "Level: Beginner"

    // ── Settings ──────────────────────────────────────────────────────
    [Header("Settings")]
    public string      loginSceneName = "Login-Scene";

    // ── State ─────────────────────────────────────────────────────────
    private bool   panelOpen = false;
    private Texture2D cachedPhoto = null;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Start()
    {
        // Wire buttons
        if (profileButton != null) profileButton.onClick.AddListener(ToggleDashboard);
        if (closeButton   != null) closeButton.onClick.AddListener(CloseDashboard);
        if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOut);

        // Close panel on start
        if (dashboardPanel != null) dashboardPanel.SetActive(false);

        // Populate identity from UserSession
        PopulateProfile();

        // Subscribe to Agent 1 live updates (may need retry if LPM not ready yet)
        TrySubscribeToLPM();
    }

    private bool _subscribedToLPM = false;

    private void TrySubscribeToLPM()
    {
        if (_subscribedToLPM) return;
        if (LearnerProfileManager.Instance == null) return;

        _subscribedToLPM = true;
        LearnerProfileManager.Instance.OnProfileUpdated += OnProfileUpdated;
        LearnerProfileManager.Instance.OnSummaryUpdated += OnSummaryUpdated;

        // If already loaded, populate now
        if (LearnerProfileManager.Instance.IsReady)
        {
            OnProfileUpdated(LearnerProfileManager.Instance.Profile);
            OnSummaryUpdated(LearnerProfileManager.Instance.Summary);
        }
    }

    private void Update()
    {
        // Retry subscription if LPM wasn't available at Start()
        if (!_subscribedToLPM)
            TrySubscribeToLPM();
    }

    private void OnDestroy()
    {
        if (_subscribedToLPM && LearnerProfileManager.Instance != null)
        {
            LearnerProfileManager.Instance.OnProfileUpdated -= OnProfileUpdated;
            LearnerProfileManager.Instance.OnSummaryUpdated -= OnSummaryUpdated;
        }
    }

    // ── Agent 1 callbacks ─────────────────────────────────────────────

    private void OnProfileUpdated(LearnerProfile profile)
    {
        if (profile == null) return;

        // Prefer the name/email from the backend profile; fall back to
        // UserSession (which has the Google/demo name even if the backend
        // document hasn't been updated yet).
        string name  = !string.IsNullOrEmpty(profile.displayName)
            ? profile.displayName
            : UserSession.Instance?.DisplayName;
        string email = !string.IsNullOrEmpty(profile.email)
            ? profile.email
            : UserSession.Instance?.Email;

        if (!string.IsNullOrEmpty(name)  && nameText  != null) nameText.text  = name;
        if (!string.IsNullOrEmpty(email) && emailText != null) emailText.text = email;

        var s = profile.agentSummary;
        if (s == null) return;

        if (organsStudiedLabel != null)
            organsStudiedLabel.text = $"Organs studied: {s.organsStudiedCount}";

        if (levelLabel != null)
        {
            string raw = !string.IsNullOrEmpty(s.overallLevel) ? s.overallLevel : "beginner";
            string lvl = char.ToUpper(raw[0]) + raw.Substring(1);
            levelLabel.text = $"Level: {lvl}";
        }

        if (recommendedLabel != null && !string.IsNullOrEmpty(s.recommendedNextOrgan))
        {
            string next = char.ToUpper(s.recommendedNextOrgan[0]) + s.recommendedNextOrgan.Substring(1);
            recommendedLabel.text = $"Next: {next}";
        }
    }

    private void OnSummaryUpdated(AgentSummary summary)
    {
        if (summary == null) return;

        // Quiz score — average across all quizzes for a quick indicator
        if (quizScoreLabel != null)
        {
            var history = LearnerProfileManager.Instance?.Profile?.quizHistory;
            if (history != null && history.Count > 0)
            {
                float avg = 0f;
                foreach (var q in history) avg += q.percentage;
                avg /= history.Count;
                quizScoreLabel.text = $"Avg quiz score: {avg:0}%";
            }
            else
            {
                quizScoreLabel.text = "Quiz score: No quizzes taken yet";
            }
        }

        // Weak concepts — show up to 2
        if (weakConceptsLabel != null)
        {
            if (summary.weakConcepts != null && summary.weakConcepts.Count > 0)
            {
                int show = Mathf.Min(2, summary.weakConcepts.Count);
                string weak = string.Join(", ", summary.weakConcepts.GetRange(0, show));
                if (summary.weakConcepts.Count > 2) weak += "…";
                weakConceptsLabel.text = $"Weak: {weak}";
            }
            else
            {
                weakConceptsLabel.text = "Weak concepts: Take a quiz to find out";
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────

    public void ToggleDashboard()
    {
        if (panelOpen) CloseDashboard();
        else           OpenDashboard();
    }

    public void OpenDashboard()
    {
        if (dashboardPanel == null) return;
        dashboardPanel.SetActive(true);
        panelOpen = true;

        // Refresh from backend whenever the dashboard opens
        TryRefreshFromBackend();
    }

    public void CloseDashboard()
    {
        if (dashboardPanel == null) return;
        dashboardPanel.SetActive(false);
        panelOpen = false;
    }

    // ── Sign out ──────────────────────────────────────────────────────

    private void OnSignOut()
    {
        if (UserSession.Instance != null)
            UserSession.Instance.SignOut();

        SceneManager.LoadScene(loginSceneName);
    }

    // ── Populate UI from UserSession ──────────────────────────────────

    private void PopulateProfile()
    {
        if (UserSession.Instance == null || !UserSession.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[ProfileDashboard] No active session.");
            return;
        }

        var session = UserSession.Instance;

        // Text fields
        if (nameText  != null) nameText.text  = session.IsGuest ? "Guest Student" : session.DisplayName;
        if (emailText != null) emailText.text = session.IsGuest ? "Not signed in" : session.Email;

        // Show short UID (e.g. first 12 chars) – full UID used by Agent 1 internally
        if (userIdText != null)
            userIdText.text = session.IsGuest
                ? "ID: guest"
                : "ID: " + (session.UserId.Length > 12
                    ? session.UserId.Substring(0, 12) + "…"
                    : session.UserId);

        // Guest / Google badge
        if (guestBadge  != null) guestBadge.SetActive(session.IsGuest);
        if (googleBadge != null) googleBadge.SetActive(!session.IsGuest);

        // Load profile photo
        if (!string.IsNullOrEmpty(session.PhotoUrl))
            StartCoroutine(LoadProfilePhoto(session.PhotoUrl));

        // Set default text for stat labels (updated when Agent 1 data arrives)
        if (organsStudiedLabel != null) organsStudiedLabel.text = "Organs studied: 0";
        if (quizScoreLabel     != null) quizScoreLabel.text     = "Quiz score: No quizzes taken yet";
        if (weakConceptsLabel  != null) weakConceptsLabel.text  = "Weak concepts: Take a quiz to find out";
        if (levelLabel         != null) levelLabel.text         = "Level: Beginner";
        if (recommendedLabel   != null) recommendedLabel.text   = "Next: Scan an organ to start";
    }

    // ── Photo download ────────────────────────────────────────────────

    private IEnumerator LoadProfilePhoto(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            cachedPhoto = DownloadHandlerTexture.GetContent(req);

            if (profilePhoto  != null) profilePhoto.texture  = cachedPhoto;
            if (hudAvatarImage != null) hudAvatarImage.texture = cachedPhoto;
        }
        else
        {
            Debug.LogWarning($"[ProfileDashboard] Could not load photo: {req.error}");
        }
    }

    // ── Refresh from backend GET endpoint ─────────────────────────────

    private void TryRefreshFromBackend()
    {
        if (UserSession.Instance == null || !UserSession.Instance.IsLoggedIn) return;
        if (LearnerProfileManager.Instance == null) return;

        StartCoroutine(LearnerProfileManager.Instance.RefreshProfileFromBackend(
            profile =>
            {
                OnProfileUpdated(profile);
                if (profile?.agentSummary != null)
                    OnSummaryUpdated(profile.agentSummary);
            }
        ));
    }
}
