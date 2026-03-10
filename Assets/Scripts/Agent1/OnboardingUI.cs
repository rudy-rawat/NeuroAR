using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Shown once to new students right after first login.
/// Collects: Grade, Prior knowledge level, Learning goal.
/// Writes answers into LearnerProfileManager and saves to backend.
/// </summary>
public class OnboardingUI : MonoBehaviour
{
    [Header("Step panels — one per question")]
    public GameObject stepGrade;           // Step 1: which class?
    public GameObject stepKnowledge;       // Step 2: prior knowledge?
    public GameObject stepGoal;            // Step 3: learning goal?
    public GameObject stepDone;            // Final: welcome screen

    [Header("Step 1 — Grade buttons")]
    public Button btnGrade9;
    public Button btnGrade10;
    public Button btnGrade11;
    public Button btnGrade12;

    [Header("Step 2 — Knowledge buttons")]
    public Button btnBeginner;
    public Button btnIntermediate;
    public Button btnAdvanced;

    [Header("Step 3 — Goal buttons")]
    public Button btnExam;
    public Button btnCuriosity;
    public Button btnRevision;

    [Header("Progress")]
    public TextMeshProUGUI stepLabel;   // "Step 1 of 3"

    [Header("Navigation")]
    public string homeSceneName = "Start-Scene";

    // ── State ─────────────────────────────────────────────────────────
    private string _grade;
    private string _knowledge;
    private string _goal;

    // ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Guard — if profile already complete, skip onboarding
        if (LearnerProfileManager.Instance != null
            && LearnerProfileManager.Instance.IsReady
            && LearnerProfileManager.Instance.Profile?.onboarding?.completed == true)
        {
            SceneManager.LoadScene(homeSceneName);
            return;
        }

        ShowStep(1);
        WireButtons();
    }

    // ── Button wiring ─────────────────────────────────────────────────

    private void WireButtons()
    {
        btnGrade9?.onClick.AddListener(() => SelectGrade("Class 9"));
        btnGrade10?.onClick.AddListener(() => SelectGrade("Class 10"));
        btnGrade11?.onClick.AddListener(() => SelectGrade("Class 11"));
        btnGrade12?.onClick.AddListener(() => SelectGrade("Class 12"));

        btnBeginner?.onClick.AddListener(() => SelectKnowledge("beginner"));
        btnIntermediate?.onClick.AddListener(() => SelectKnowledge("intermediate"));
        btnAdvanced?.onClick.AddListener(() => SelectKnowledge("advanced"));

        btnExam?.onClick.AddListener(() => SelectGoal("exam preparation"));
        btnCuriosity?.onClick.AddListener(() => SelectGoal("curiosity"));
        btnRevision?.onClick.AddListener(() => SelectGoal("revision"));
    }

    // ── Handlers ──────────────────────────────────────────────────────

    private void SelectGrade(string grade)
    {
        _grade = grade;
        ShowStep(2);
    }

    private void SelectKnowledge(string level)
    {
        _knowledge = level;
        ShowStep(3);
    }

    private void SelectGoal(string goal)
    {
        _goal = goal;
        ShowStep(4);  // "done" screen
        SubmitOnboarding();
    }

    // ── Submit ────────────────────────────────────────────────────────

    private void SubmitOnboarding()
    {
        if (LearnerProfileManager.Instance == null || !LearnerProfileManager.Instance.IsReady)
        {
            Debug.LogWarning("[OnboardingUI] LearnerProfileManager not ready. Navigating anyway.");
            Invoke(nameof(GoHome), 1.5f);
            return;
        }

        var profile = LearnerProfileManager.Instance.Profile;

        // Stamp identity from UserSession so the backend document always has
        // the correct displayName/email (the blank profile created on first
        // login has these empty).
        var session = UserSession.Instance;
        if (session != null)
        {
            if (!string.IsNullOrEmpty(session.DisplayName)) profile.displayName = session.DisplayName;
            if (!string.IsNullOrEmpty(session.Email))       profile.email       = session.Email;
            if (!string.IsNullOrEmpty(session.PhotoUrl))    profile.photoUrl    = session.PhotoUrl;
        }

        profile.onboarding = new OnboardingData
        {
            completed      = true,
            grade          = _grade,
            priorKnowledge = _knowledge,
            learningGoal   = _goal
        };

        // Also prime Agent 1's summary level from the onboarding answer
        profile.agentSummary.overallLevel = _knowledge;

        // Save to backend
        StartCoroutine(new ProfileService(LearnerProfileManager.Instance.azureFunctionsBaseUrl)
            .SaveProfile(profile,
                onSuccess: () =>
                {
                    Debug.Log("[OnboardingUI] Onboarding saved.");
                    Invoke(nameof(GoHome), 1.5f);
                },
                onError: err =>
                {
                    Debug.LogWarning($"[OnboardingUI] Save failed (offline): {err}");
                    Invoke(nameof(GoHome), 1.5f);
                }
            )
        );
    }

    private void GoHome() => SceneManager.LoadScene(homeSceneName);

    // ── Step navigation ───────────────────────────────────────────────

    private void ShowStep(int step)
    {
        if (stepGrade     != null) stepGrade.SetActive(step == 1);
        if (stepKnowledge != null) stepKnowledge.SetActive(step == 2);
        if (stepGoal      != null) stepGoal.SetActive(step == 3);
        if (stepDone      != null) stepDone.SetActive(step == 4);

        if (stepLabel != null && step <= 3)
            stepLabel.text = $"Step {step} of 3";
    }
}
