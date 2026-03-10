using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// HTTP layer for Agent 3 — requests GPT-generated, personalised quiz questions
/// from Azure Functions based on the student's weak concepts and learning level.
/// </summary>
public class QuizService : MonoBehaviour
{
    private string _baseUrl;

    public void Init(string baseUrl) => _baseUrl = baseUrl;

    // ── Request / Response models ────────────────────────────────────────────

    [Serializable]
    public class QuestionRequest
    {
        public string   userId;
        public string   organName;
        public string   level;          // "beginner" | "intermediate" | "advanced"
        public string[] weakConcepts;   // topics Agent 1 identified as weak
        public int      count;          // how many questions to generate (default 5)
    }

    [Serializable]
    public class GeneratedQuestion
    {
        public string   question;
        public string[] options;            // always 4 options
        public int      correctAnswerIndex; // 0-3
        public string   explanation;        // explanation of the correct answer
    }

    [Serializable]
    public class QuestionResponse
    {
        public GeneratedQuestion[] questions;
        public string              source;  // "agent" | "fallback"
    }

    // ── HTTP call ────────────────────────────────────────────────────────────

    public IEnumerator RequestQuestions(
        QuestionRequest req,
        Action<QuestionResponse> onSuccess,
        Action<string> onError)
    {
        string url  = _baseUrl.TrimEnd('/') + "/api/agent/question";
        string json = JsonConvert.SerializeObject(req);

        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.timeout = 30; // LLM generation can take time

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string body = www.downloadHandler.text;
            Debug.Log($"[QuizService] Raw response ({body.Length} chars): {body.Substring(0, Mathf.Min(200, body.Length))}");
            try
            {
                var response = JsonConvert.DeserializeObject<QuestionResponse>(body);
                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuizService] Parse error: {e.Message}\nBody: {body}");
                onError?.Invoke($"Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[QuizService] HTTP {www.responseCode} — {www.error}");
            onError?.Invoke($"HTTP {www.responseCode} — {www.error}");
        }
    }

    /// <summary>Converts Agent 3 response into QuizQuestion[] used by QuizUI.</summary>
    public static QuizQuestion[] ToQuizQuestions(GeneratedQuestion[] generated)
    {
        var result = new QuizQuestion[generated.Length];
        for (int i = 0; i < generated.Length; i++)
        {
            result[i] = new QuizQuestion
            {
                question           = generated[i].question,
                options            = generated[i].options,
                correctAnswerIndex = generated[i].correctAnswerIndex
            };
        }
        return result;
    }
}
