using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class RoadmapUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text overallFocusText;

    [Header("Body Text")]
    public TMP_Text roadmapBodyText;

    [Header("Typography")]
    public int headingFontSize = 44;
    public int bodyFontSize = 28;

    [Header("Body Panel")]
    public Color bodyPanelColor = new Color(0.10f, 0.14f, 0.22f, 0.95f);

    private void Start()
    {
        ConfigureResponsiveCanvas();
        EnsureSimpleTextLayout();

        if (overallFocusText != null)
        {
            overallFocusText.text = "Loading Learning Roadmap...";
            ApplyHeaderStyle();
        }

        LoadRoadmap();
    }

    public void LoadRoadmap(bool forceRefresh = false)
    {
        if (RoadmapService.Instance == null)
        {
            Debug.LogError("RoadmapService is not in the scene!");
            if (overallFocusText != null) overallFocusText.text = "Error: RoadmapService missing.";
            if (roadmapBodyText != null) roadmapBodyText.text = "Roadmap service object not found in this scene.";
            return;
        }

        string userId;
        if (!EnsureUserId(out userId))
        {
            if (overallFocusText != null) overallFocusText.text = "Unable to start session. Please login again.";
            if (roadmapBodyText != null) roadmapBodyText.text = "No user session available.";
            Debug.LogError("RoadmapUI: Could not resolve a valid UserId.");
            return;
        }

        if (overallFocusText != null)
        {
            overallFocusText.text = forceRefresh ? "Generating new roadmap..." : "Loading current roadmap...";
        }

        if (roadmapBodyText != null)
        {
            roadmapBodyText.text = "Fetching roadmap from backend...";
        }

        RoadmapService.Instance.GetRoadmap(userId, OnRoadmapLoaded, OnRoadmapError, forceRefresh);
    }

    private bool EnsureUserId(out string userId)
    {
        userId = string.Empty;

        if (UserSession.Instance == null)
        {
            var go = new GameObject("UserSession");
            go.AddComponent<UserSession>();
        }

        if (UserSession.Instance == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(UserSession.Instance.UserId))
        {
            UserSession.Instance.SetGuest();
        }

        userId = UserSession.Instance.UserId;
        return !string.IsNullOrEmpty(userId);
    }

    private void OnRoadmapLoaded(RoadmapResponse response)
    {
        if (response == null || response.steps == null)
        {
            if (overallFocusText != null) overallFocusText.text = "Failed to load roadmap data.";
            if (roadmapBodyText != null) roadmapBodyText.text = "Backend returned an empty roadmap payload.";
            return;
        }

        Debug.Log($"[RoadmapUI] Loaded roadmap: steps={response.steps.Count}, focusLen={(response.overallFocus ?? string.Empty).Length}");

        if (overallFocusText != null)
        {
            overallFocusText.text = "Your Personalized Learning Roadmap";
            ApplyHeaderStyle();
        }

        if (roadmapBodyText != null)
        {
            roadmapBodyText.text = BuildRoadmapText(response);
            Debug.Log($"[RoadmapUI] Text box content length: {roadmapBodyText.text.Length}");
        }
    }

    private void OnRoadmapError(string errorMsg)
    {
        if (overallFocusText != null) overallFocusText.text = "Error loading roadmap";
        if (roadmapBodyText != null) roadmapBodyText.text = $"Error loading roadmap:\n{errorMsg}";
    }

    private void ApplyHeaderStyle()
    {
        if (overallFocusText == null) return;
        overallFocusText.fontSize = headingFontSize;
        overallFocusText.fontStyle = FontStyles.Bold;
        overallFocusText.textWrappingMode = TextWrappingModes.Normal;
        overallFocusText.color = Color.white;
    }

    private void ConfigureResponsiveCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
    }

    private void EnsureSimpleTextLayout()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        // Disable old scroll view so there is no blank scroll region.
        Transform oldScroll = parentCanvas.transform.Find("ScrollView");
        if (oldScroll != null)
        {
            oldScroll.gameObject.SetActive(false);
        }

        if (roadmapBodyText != null) return;

        GameObject panel = new GameObject("RoadmapBodyPanel");
        panel.transform.SetParent(parentCanvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.08f);
        panelRect.anchorMax = new Vector2(0.94f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = bodyPanelColor;

        GameObject textGO = new GameObject("RoadmapBodyText");
        textGO.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(28f, 24f);
        textRect.offsetMax = new Vector2(-28f, -24f);

        roadmapBodyText = textGO.AddComponent<TextMeshProUGUI>();
        roadmapBodyText.fontSize = bodyFontSize;
        roadmapBodyText.textWrappingMode = TextWrappingModes.Normal;
        roadmapBodyText.alignment = TextAlignmentOptions.TopLeft;
        roadmapBodyText.color = new Color(0.92f, 0.96f, 1f);
        roadmapBodyText.text = "Waiting for roadmap data...";
    }

    private string BuildRoadmapText(RoadmapResponse response)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("On the basis of your learned path and quiz knowledge, follow these steps:");
        sb.AppendLine();
        sb.AppendLine($"Focus: {response.overallFocus}");

        if (!string.IsNullOrEmpty(response.learningNarrative))
        {
            sb.AppendLine();
            sb.AppendLine(response.learningNarrative);
        }

        if (!string.IsNullOrEmpty(response.studyAdvice))
        {
            sb.AppendLine();
            sb.AppendLine($"Study Advice: {response.studyAdvice}");
        }

        sb.AppendLine();
        sb.AppendLine("------------------------------");

        if (response.steps != null)
        {
            foreach (var step in response.steps)
            {
                sb.AppendLine();
                sb.AppendLine($"Step {step.stepNumber}: {step.topic}");
                sb.AppendLine($"Time: {step.estimatedTimeInMinutes} minutes");
                sb.AppendLine(step.description);

                if (step.resources != null && step.resources.Count > 0)
                {
                    sb.AppendLine("Resources:");
                    foreach (var res in step.resources)
                    {
                        if (res.type == "external_link")
                        {
                            sb.AppendLine($"- {res.name} [Link]: {res.url}");
                        }
                        else if (res.type == "organ_scene")
                        {
                            sb.AppendLine($"- {res.name} [3D Organ]: {res.referenceId}");
                        }
                        else
                        {
                            sb.AppendLine($"- {res.name} [{res.type}]");
                        }
                    }
                }

                sb.AppendLine("------------------------------");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Roadmap ID: {response.roadmapId}");
        sb.AppendLine($"Generated At: {response.generatedAt}");

        return sb.ToString();
    }
}
