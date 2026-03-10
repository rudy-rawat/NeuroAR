using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>HTTP layer for Agent 2 — sends narration requests to Azure Functions.</summary>
public class NarrationService : MonoBehaviour
{
    private string _baseUrl;

    public void Init(string baseUrl) => _baseUrl = baseUrl;

    [Serializable]
    public class NarrationRequest
    {
        public string   userId;
        public string   organName;
        public string   level;
        public string[] weakConcepts;
        public int      sessionCount;
        public int      pageIndex;    // 0 = intro, 1 = structure, 2 = function, etc.
    }

    [Serializable]
    public class NarrationResponse
    {
        public string narrationText;
        public string audioUrl;   // reserved for future audio support
    }

    public IEnumerator RequestNarration(
        NarrationRequest req,
        Action<NarrationResponse> onSuccess,
        Action<string> onError)
    {
        string url  = _baseUrl.TrimEnd('/') + "/api/agent/narrate";
        string json = JsonConvert.SerializeObject(req);

        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.timeout = 20;

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string body = www.downloadHandler.text;
            Debug.Log($"[NarrationService] Response: {body.Substring(0, Mathf.Min(200, body.Length))}");
            try
            {
                var response = JsonConvert.DeserializeObject<NarrationResponse>(body);
                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NarrationService] Parse error: {e.Message}\nBody: {body}");
                onError?.Invoke($"Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[NarrationService] HTTP {www.responseCode} — {www.error}");
            onError?.Invoke($"HTTP {www.responseCode} — {www.error}");
        }
    }
}
