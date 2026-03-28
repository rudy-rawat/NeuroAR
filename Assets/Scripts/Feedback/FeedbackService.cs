using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class FeedbackService : MonoBehaviour
{
    public static FeedbackService Instance { get; private set; }

    [SerializeField]
    public string baseUrl = "https://neuroar-apb0bnbwgvaqf2b4.centralindia-01.azurewebsites.net/";

    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

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
        }
    }

    public void SyncBaseUrlFromProfileManager()
    {
        if (LearnerProfileManager.Instance != null && !string.IsNullOrEmpty(LearnerProfileManager.Instance.azureFunctionsBaseUrl))
        {
            baseUrl = LearnerProfileManager.Instance.azureFunctionsBaseUrl.TrimEnd('/');
        }
        else
        {
            baseUrl = baseUrl.TrimEnd('/');
        }
    }

    public void SubmitFeedback(FeedbackRequest request, Action<FeedbackResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(SubmitFeedbackRoutine(request, onSuccess, onError));
    }

    private IEnumerator SubmitFeedbackRoutine(FeedbackRequest request, Action<FeedbackResponse> onSuccess, Action<string> onError)
    {
        if (request == null || string.IsNullOrEmpty(request.userId) || string.IsNullOrEmpty(request.suggestedAction))
        {
            onError?.Invoke("Invalid feedback request.");
            yield break;
        }

        SyncBaseUrlFromProfileManager();

        string payload = JsonConvert.SerializeObject(request, _jsonSettings);
        using var req = new UnityWebRequest(baseUrl + "/api/feedback", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");
        req.timeout = 20;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject<FeedbackResponse>(req.downloadHandler.text, _jsonSettings);
                onSuccess?.Invoke(parsed);
            }
            catch (Exception ex)
            {
                onError?.Invoke("Feedback parse error: " + ex.Message);
            }
        }
        else
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            onError?.Invoke($"HTTP {req.responseCode} — {req.error} {body}");
        }
    }
}
