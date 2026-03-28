using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton — the Unity-side brain of Agent 1.
///
/// Responsibilities:
///   1. On login:  calls ProfileService.LoadProfile  → populates Profile + AgentSummary
///   2. On organ viewed:  calls ProfileService.LogOrganSession → updates profile
///   3. On quiz done:  called by QuizService.SubmitQuiz → updates profile
///   4. Provides AgentSummary to Agent 2 (narration level) and Agent 3 (weak concepts)
///   5. Exposes events so Dashboard UI refreshes whenever data changes
///
/// Usage anywhere:
///   LearnerProfileManager.Instance.Profile          — full document
///   LearnerProfileManager.Instance.Summary          — Agent 1's computed summary
///   LearnerProfileManager.Instance.IsReady          — true after first load
/// </summary>
public class LearnerProfileManager : MonoBehaviour
{
    public static LearnerProfileManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Azure Functions Base URL")]
    [Tooltip("e.g. https://your-app.azurewebsites.net  (no trailing slash)")]
    public string azureFunctionsBaseUrl = "https://neuroar-apb0bnbwgvaqf2b4.centralindia-01.azurewebsites.net/";

    [Header("Settings")]
    [Tooltip("Scene to load after profile is ready and onboarding is done.")]
    public string homeSceneName = "Start-Scene";

    // ── State ─────────────────────────────────────────────────────────
    public LearnerProfile Profile  { get; private set; }
    public AgentSummary   Summary  => Profile?.agentSummary;
    public bool           IsReady  { get; private set; } = false;
    public bool           IsLoading{ get; private set; } = false;

    // ── Events ────────────────────────────────────────────────────────
    /// Fired after profile loads or updates — Dashboard subscribes to this
    public event Action<LearnerProfile> OnProfileUpdated;
    /// Fired when Agent 1 finishes computing a new summary
    public event Action<AgentSummary>   OnSummaryUpdated;

    private ProfileService _service;
    // Lazy: created on first use so that azureFunctionsBaseUrl can be set
    // AFTER AddComponent() (e.g. from LoginManager programmatic fallback).
    private ProfileService Service => _service ??= new ProfileService(azureFunctionsBaseUrl);

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Call this right after UserSession is set (from LoginManager).
    /// Loads or creates the learner profile from Cosmos DB.
    /// </summary>
    public void LoadProfileForCurrentUser(Action onDone = null, Action<string> onError = null)
    {
        if (UserSession.Instance == null || !UserSession.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[LearnerProfileManager] No active user session.");
            onError?.Invoke("Not logged in");
            return;
        }

        StartCoroutine(LoadProfileCoroutine(onDone, onError));
    }

    /// <summary>
    /// Call when a student finishes viewing an organ (target lost or scene change).
    /// </summary>
    public void LogOrganSession(string organName, int timeSeconds,
        bool viewedBasic, bool viewedDetailed, bool viewedLabels, bool viewedInfo)
    {
        if (!IsReady) return;

        var req = new ProfileService.OrganLogRequest
        {
            userId           = UserSession.Instance.UserId,
            organName        = organName.ToLower(),
            addTimeSeconds   = timeSeconds,
            viewedBasic      = viewedBasic,
            viewedDetailed   = viewedDetailed,
            viewedLabels     = viewedLabels,
            viewedInfo       = viewedInfo
        };

        StartCoroutine(Service.LogOrganSession(req,
            updatedProfile =>
            {
                Profile = updatedProfile;
                OnProfileUpdated?.Invoke(Profile);
                OnSummaryUpdated?.Invoke(Profile.agentSummary);
                Debug.Log($"[LearnerProfileManager] Organ session logged: {organName}");
            },
            err => Debug.LogWarning($"[LearnerProfileManager] LogOrganSession failed: {err}")
        ));
    }

    /// <summary>
    /// Call after a quiz completes. Returns explanations for wrong answers via callback.
    /// </summary>
    public void SubmitQuizResult(QuizResult result,
        Action<ProfileService.QuizSubmitResponse> onDone = null,
        Action<string> onError = null)
    {
        if (!IsReady) return;

        var req = new ProfileService.QuizSubmitRequest
        {
            userId = UserSession.Instance.UserId,
            result = result
        };

        StartCoroutine(Service.SubmitQuiz(req,
            response =>
            {
                Profile = response.updatedProfile;
                OnProfileUpdated?.Invoke(Profile);
                OnSummaryUpdated?.Invoke(Profile.agentSummary);
                Debug.Log($"[LearnerProfileManager] Quiz submitted for {result.organName}, score {result.score}/{result.totalQuestions}");
                onDone?.Invoke(response);
            },
            err =>
            {
                Debug.LogWarning($"[LearnerProfileManager] SubmitQuiz failed: {err}");
                onError?.Invoke(err);
            }
        ));
    }

    /// <summary>
    /// Returns the current learning level for Agent 2 to use for narration.
    /// Falls back to "beginner" if profile not yet loaded.
    /// </summary>
    public string GetLearningLevel() =>
        Summary?.overallLevel ?? "beginner";

    /// <summary>
    /// Returns weak concepts list for Agent 3 to target in quiz generation.
    /// </summary>
    public System.Collections.Generic.List<string> GetWeakConcepts() =>
        Summary?.weakConcepts ?? new System.Collections.Generic.List<string>();

    /// <summary>
    /// Fetches the latest profile from the backend via GET (read-only).
    /// Used by the dashboard to refresh on-demand without side effects.
    /// </summary>
    public IEnumerator RefreshProfileFromBackend(Action<LearnerProfile> onDone)
    {
        if (UserSession.Instance == null || !UserSession.Instance.IsLoggedIn)
        {
            onDone?.Invoke(Profile);
            yield break;
        }

        yield return Service.GetProfile(UserSession.Instance.UserId,
            profile =>
            {
                Profile = profile;
                IsReady = true;
                OnProfileUpdated?.Invoke(Profile);
                OnSummaryUpdated?.Invoke(Profile?.agentSummary);
                onDone?.Invoke(Profile);
            },
            err =>
            {
                Debug.LogWarning($"[LearnerProfileManager] RefreshProfile failed: {err}");
                onDone?.Invoke(Profile); // Return cached profile
            }
        );
    }

    // ─────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator LoadProfileCoroutine(Action onDone, Action<string> onError)
    {
        IsLoading = true;
        IsReady   = false;

        string userId = UserSession.Instance.UserId;
        Debug.Log($"[LearnerProfileManager] Loading profile for userId: {userId}");

        bool failed = false;
        string errorMsg = "";

        yield return Service.LoadProfile(userId,
            profile =>
            {
                Profile = profile;
                IsReady  = true;
                // Sync UserSession display fields from the backend document
                // (handles the case where the session was restored from PlayerPrefs
                // but the backend has the authoritative displayName/email/photoUrl)
                if (!string.IsNullOrEmpty(profile.displayName) && UserSession.Instance != null)
                    UserSession.Instance.SetUser(
                        profile.userId,
                        profile.displayName,
                        profile.email,
                        profile.photoUrl ?? UserSession.Instance.PhotoUrl ?? string.Empty
                    );
                Debug.Log($"[LearnerProfileManager] Profile loaded. Level: {profile.agentSummary?.overallLevel}, Onboarding: {profile.onboarding?.completed}");
            },
            err =>
            {
                failed  = true;
                errorMsg = err;
                Debug.LogWarning($"[LearnerProfileManager] LoadProfile failed: {err}. Using offline fallback.");
            }
        );

        IsLoading = false;

        if (failed)
        {
            // Offline fallback — create a blank local profile so app still works
            Profile  = CreateLocalProfile();
            IsReady  = true;
            OnProfileUpdated?.Invoke(Profile);
            onError?.Invoke(errorMsg);
        }
        else
        {
            OnProfileUpdated?.Invoke(Profile);
            OnSummaryUpdated?.Invoke(Profile.agentSummary);
            onDone?.Invoke();
        }
    }

    /// <summary>
    /// Creates a minimal local profile when the backend is unreachable.
    /// </summary>
    private LearnerProfile CreateLocalProfile()
    {
        var session = UserSession.Instance;
        return new LearnerProfile
        {
            id          = session.UserId,
            userId      = session.UserId,
            displayName = session.DisplayName,
            email       = session.Email,
            photoUrl    = session.PhotoUrl,
            isGuest     = session.IsGuest,
            createdAt   = DateTime.UtcNow.ToString("o"),
            lastActiveAt = DateTime.UtcNow.ToString("o"),
            onboarding  = new OnboardingData { completed = false },
            agentSummary = new AgentSummary  { overallLevel = "beginner" }
        };
    }
}
