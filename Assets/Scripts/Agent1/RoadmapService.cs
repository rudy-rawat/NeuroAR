using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class RoadmapService : MonoBehaviour
{
    public static RoadmapService Instance { get; private set; }

    [SerializeField]
    public string baseUrl = "https://neuroar-apb0bnbwgvaqf2b4.centralindia-01.azurewebsites.net/"; // Adjust to actual backend URL or Production URL

    private void Awake()
    {
        // Force URL override in case an old local URL got saved in the Unity Inspector
        baseUrl = "https://neuroar-apb0bnbwgvaqf2b4.centralindia-01.azurewebsites.net/";

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GetRoadmap(string userId, Action<RoadmapResponse> onSuccess, Action<string> onError, bool forceRefresh = false)
    {
        StartCoroutine(GetRoadmapRoutine(userId, forceRefresh, onSuccess, onError));
    }

    [Serializable]
    private class ApiError
    {
        public string error;
    }

    private IEnumerator GetRoadmapRoutine(string userId, bool forceRefresh, Action<RoadmapResponse> onSuccess, Action<string> onError)
    {
        string endpoint = forceRefresh ? "roadmap" : "roadmap-existing-or-generate";
        string url = $"{baseUrl.TrimEnd('/')}/api/agent/{endpoint}/{userId}";

        bool retriedAfterProfileLoad = false;

        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 30; // Wait longer for LLM generation
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        RoadmapResponse response = ParseRoadmapResponse(json, forceRefresh);
                        if (response == null)
                        {
                            throw new Exception("Roadmap payload was empty or malformed.");
                        }

                        onSuccess?.Invoke(response);
                        yield break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"RoadmapService Parse Error: {e.Message}");
                        onError?.Invoke(e.Message);
                        yield break;
                    }
                }

                string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string apiError = ExtractApiError(body);

                // Fresh users can hit this path if profile was never created on backend.
                if (!retriedAfterProfileLoad && request.responseCode == 404 && IsUserMissingError(apiError))
                {
                    bool recovered = false;
                    string recoveryError = string.Empty;
                    yield return LoadOrCreateProfile(userId,
                        () => recovered = true,
                        err => recoveryError = err);

                    if (recovered)
                    {
                        retriedAfterProfileLoad = true;
                        continue;
                    }

                    Debug.LogError($"RoadmapService Recovery Error: {recoveryError}");
                    onError?.Invoke($"Profile recovery failed: {recoveryError}");
                    yield break;
                }

                string err = $"HTTP {request.responseCode} — {request.error}";
                if (!string.IsNullOrWhiteSpace(apiError))
                {
                    err += $" ({apiError})";
                }

                Debug.LogError($"RoadmapService Error: {err}");
                onError?.Invoke(err);
                yield break;
            }
        }
    }

    private IEnumerator LoadOrCreateProfile(string userId, Action onSuccess, Action<string> onError)
    {
        var session = UserSession.Instance;
        string payload = JsonConvert.SerializeObject(new
        {
            userId,
            displayName = session?.DisplayName ?? "",
            email = session?.Email ?? "",
            photoUrl = session?.PhotoUrl ?? "",
        });

        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl.TrimEnd('/')}/api/profile/load", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 20;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                string apiError = ExtractApiError(body);
                string err = $"HTTP {request.responseCode} — {request.error}";
                if (!string.IsNullOrWhiteSpace(apiError))
                {
                    err += $" ({apiError})";
                }
                onError?.Invoke(err);
            }
        }
    }

    private static string ExtractApiError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        try
        {
            var parsed = JsonConvert.DeserializeObject<ApiError>(body);
            return parsed?.error ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsUserMissingError(string apiError)
    {
        if (string.IsNullOrWhiteSpace(apiError)) return false;
        string normalized = apiError.ToLowerInvariant();
        return normalized.Contains("user not found") || normalized.Contains("profile not found");
    }

    private static RoadmapResponse ParseRoadmapResponse(string json, bool forceRefresh)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        if (forceRefresh)
        {
            return JsonConvert.DeserializeObject<RoadmapResponse>(json);
        }

        // Preferred response from /roadmap-existing-or-generate: { source, roadmap }
        RoadmapEnvelopeResponse envelope = JsonConvert.DeserializeObject<RoadmapEnvelopeResponse>(json);
        if (envelope != null && envelope.roadmap != null)
        {
            envelope.roadmap.source = envelope.source;
            return envelope.roadmap;
        }

        // Backward-compatible fallback if backend returns a direct roadmap object.
        RoadmapResponse directRoadmap = JsonConvert.DeserializeObject<RoadmapResponse>(json);
        if (directRoadmap != null && string.IsNullOrEmpty(directRoadmap.source))
        {
            directRoadmap.source = "generated";
        }

        return directRoadmap;
    }
}
