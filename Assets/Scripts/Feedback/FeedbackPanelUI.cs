using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FeedbackPanelUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;
    public Button openButton;
    public Button closeButton;

    [Header("Form")]
    public TMP_InputField feedbackInput;
    public TMP_Text statusText;

    [Header("Action Buttons")]
    public Button tooComplexBtn;
    public Button tooEasyBtn;
    public Button slowerPaceBtn;
    public Button fasterPaceBtn;
    public Button moderatePaceBtn;
    public Button moreVisualBtn;
    public Button lessVisualBtn;

    [Header("Context")]
    public string agentId = "anatomy_tutor_agent";

    private bool _sending = false;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        BindAction(tooComplexBtn, "decrease_complexity");
        BindAction(tooEasyBtn, "increase_complexity");
        BindAction(slowerPaceBtn, "slower_pace");
        BindAction(fasterPaceBtn, "faster_pace");
        BindAction(moderatePaceBtn, "set_pace_moderate");
        BindAction(moreVisualBtn, "enable_visual_dependency");
        BindAction(lessVisualBtn, "disable_visual_dependency");

        if (FeedbackService.Instance == null)
        {
            var go = new GameObject("FeedbackService");
            go.AddComponent<FeedbackService>();
        }

        SetStatus("Share feedback to personalize AI responses.");
    }

    private void BindAction(Button btn, string action)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() => Submit(action));
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Submit(string action)
    {
        if (_sending) return;

        if (UserSession.Instance == null || string.IsNullOrEmpty(UserSession.Instance.UserId))
        {
            SetStatus("No active session. Please login/guest first.");
            return;
        }

        _sending = true;
        SetButtonsInteractable(false);
        SetStatus("Sending feedback...");

        string raw = feedbackInput != null ? feedbackInput.text : string.Empty;
        if (raw != null && raw.Length > 4000)
        {
            raw = raw.Substring(0, 4000);
        }

        var req = new FeedbackRequest
        {
            userId = UserSession.Instance.UserId,
            agentId = agentId,
            rawFeedbackText = raw,
            suggestedAction = action,
            metadata = new FeedbackMetadata
            {
                sessionId = UserSession.Instance.UserId,
                clientVersion = Application.version,
                sceneName = SceneManager.GetActiveScene().name,
                platform = Application.platform.ToString(),
            }
        };

        FeedbackService.Instance.SubmitFeedback(req,
            onSuccess: resp =>
            {
                _sending = false;
                SetButtonsInteractable(true);
                string applied = resp != null && !string.IsNullOrEmpty(resp.appliedAction) ? resp.appliedAction : "none";
                SetStatus($"Feedback saved. Applied action: {applied}");

                // Keep local profile in sync if present.
                if (LearnerProfileManager.Instance != null && LearnerProfileManager.Instance.Profile != null && resp != null && resp.learningPreferences != null)
                {
                    LearnerProfileManager.Instance.Profile.learningPreferences = resp.learningPreferences;
                }

                if (feedbackInput != null)
                {
                    feedbackInput.text = string.Empty;
                }
            },
            onError: err =>
            {
                _sending = false;
                SetButtonsInteractable(true);
                SetStatus("Feedback failed: " + err);
                Debug.LogWarning("[FeedbackPanelUI] " + err);
            });
    }

    private void SetButtonsInteractable(bool enabled)
    {
        if (tooComplexBtn != null) tooComplexBtn.interactable = enabled;
        if (tooEasyBtn != null) tooEasyBtn.interactable = enabled;
        if (slowerPaceBtn != null) slowerPaceBtn.interactable = enabled;
        if (fasterPaceBtn != null) fasterPaceBtn.interactable = enabled;
        if (moderatePaceBtn != null) moderatePaceBtn.interactable = enabled;
        if (moreVisualBtn != null) moreVisualBtn.interactable = enabled;
        if (lessVisualBtn != null) lessVisualBtn.interactable = enabled;
        if (closeButton != null) closeButton.interactable = enabled;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
}
