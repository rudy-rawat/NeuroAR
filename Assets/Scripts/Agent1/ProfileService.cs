using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Thin HTTP service that talks to the Azure Functions backend for Agent 1.
///
/// All methods are coroutines — call via StartCoroutine() or through
/// LearnerProfileManager which wraps them with callbacks.
///
/// BASE URL: set your Azure Function App URL in the Inspector on
/// LearnerProfileManager, or hardcode below for testing.
///
/// Endpoints consumed:
///   POST  /api/profile/load        → loads or creates a learner document
///   POST  /api/profile/save        → upserts the full document
///   POST  /api/organ/log           → records one organ view session
///   POST  /api/quiz/submit         → saves a completed quiz + gets explanation
/// </summary>
public class ProfileService
{
    private readonly string _baseUrl;

    // Shared JSON settings – matches backend camelCase convention
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling    = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public ProfileService(string baseUrl)
    {
        // Strip trailing slash for safety
        _baseUrl = baseUrl.TrimEnd('/');
    }

    // ─────────────────────────────────────────────────────────────────
    // GET PROFILE  (read-only fetch by userId)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the learner profile via GET (no side-effects).
    /// Returns null via onError if profile not found (404).
    /// </summary>
    public IEnumerator GetProfile(string userId, Action<LearnerProfile> onSuccess, Action<string> onError)
    {
        string url = _baseUrl + "/api/profile/" + UnityEngine.Networking.UnityWebRequest.EscapeURL(userId);

        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        req.SetRequestHeader("Accept", "application/json");
        req.timeout = 15;

        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            try
            {
                var profile = JsonConvert.DeserializeObject<LearnerProfile>(json, _jsonSettings);
                onSuccess?.Invoke(profile);
            }
            catch (Exception ex)
            {
                onError?.Invoke("Parse error: " + ex.Message);
            }
        }
        else
        {
            onError?.Invoke($"HTTP {req.responseCode} — {req.error}");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // LOAD PROFILE  (called once at login by LearnerProfileManager)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the learner profile from Cosmos DB (or creates one for new users).
    /// Also sends the current identity fields so the backend keeps them up to date.
    /// </summary>
    public IEnumerator LoadProfile(string userId, Action<LearnerProfile> onSuccess, Action<string> onError)
    {
        var session = UserSession.Instance;
        var payload = JsonConvert.SerializeObject(new
        {
            userId,
            displayName = session?.DisplayName ?? "",
            email       = session?.Email       ?? "",
            photoUrl    = session?.PhotoUrl    ?? "",
        }, _jsonSettings);
        yield return Post("/api/profile/load", payload, onSuccess, onError);
    }

    // ─────────────────────────────────────────────────────────────────
    // SAVE PROFILE  (after onboarding, or a full sync)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upserts the full learner profile document (used after onboarding completes).
    /// </summary>
    public IEnumerator SaveProfile(LearnerProfile profile, Action onSuccess, Action<string> onError)
    {
        var payload = JsonConvert.SerializeObject(profile, _jsonSettings);
        yield return Post<object>("/api/profile/save", payload,
            _ => onSuccess?.Invoke(), onError);
    }

    // ─────────────────────────────────────────────────────────────────
    // LOG ORGAN SESSION  (called when the AR target is lost / user leaves)
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public class OrganLogRequest
    {
        public string userId;
        public string organName;
        public int    addTimeSeconds;
        public bool   viewedBasic;
        public bool   viewedDetailed;
        public bool   viewedLabels;
        public bool   viewedInfo;
    }

    /// <summary>
    /// Logs one organ viewing session. Agent 2 also calls this after narration.
    /// </summary>
    public IEnumerator LogOrganSession(OrganLogRequest request,
        Action<LearnerProfile> onSuccess, Action<string> onError)
    {
        var payload = JsonConvert.SerializeObject(request, _jsonSettings);
        // Backend updates organHistory, recalculates agentSummary, returns updated profile
        yield return Post("/api/organ/log", payload, onSuccess, onError);
    }

    // ─────────────────────────────────────────────────────────────────
    // SUBMIT QUIZ  (called by QuizService after quiz ends)
    // ─────────────────────────────────────────────────────────────────

    [Serializable]
    public class QuizSubmitRequest
    {
        public string         userId;
        public QuizResult     result;
    }

    [Serializable]
    public class QuizSubmitResponse
    {
        public LearnerProfile updatedProfile;
        // Voice explanations for wrong answers (Agent 3 writes these)
        public System.Collections.Generic.List<WrongAnswerExplanation> explanations
            = new System.Collections.Generic.List<WrongAnswerExplanation>();
    }

    [Serializable]
    public class WrongAnswerExplanation
    {
        public string question;
        public string explanation;    // Short text from GPT-4o
        public string audioUrl;       // Azure AI Speech pre-signed URL (optional)
    }

    /// <summary>
    /// Submits the completed quiz. Returns updated learner profile + explanations.
    /// </summary>
    public IEnumerator SubmitQuiz(QuizSubmitRequest request,
        Action<QuizSubmitResponse> onSuccess, Action<string> onError)
    {
        var payload = JsonConvert.SerializeObject(request, _jsonSettings);
        yield return Post("/api/quiz/submit", payload, onSuccess, onError);
    }

    // ─────────────────────────────────────────────────────────────────
    // PRIVATE — generic POST helper
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator Post<T>(string endpoint, string jsonBody,
        Action<T> onSuccess, Action<string> onError)
    {
        string url = _baseUrl + endpoint;

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept",       "application/json");
        req.timeout = 30; // Bug fix: without timeout, hung requests block forever

        // Attach auth header if user is signed in (Firebase ID token future-proofing)
        // req.SetRequestHeader("Authorization", "Bearer " + idToken);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            Debug.Log($"[ProfileService] {endpoint} ✓  ({req.downloadedBytes} bytes)");

            // Handle empty / 204 No Content responses (e.g. SaveProfile)
            if (string.IsNullOrWhiteSpace(json))
            {
                onSuccess?.Invoke(default);
                yield break;
            }

            try
            {
                T result = JsonConvert.DeserializeObject<T>(json, _jsonSettings);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] JSON parse error at {endpoint}: {ex.Message}\n{json}");
                onError?.Invoke("Parse error: " + ex.Message);
            }
        }
        else
        {
            string err = $"HTTP {req.responseCode} — {req.error}";
            Debug.LogError($"[ProfileService] {endpoint} ✗  {err}");
            onError?.Invoke(err);
        }
    }
}
