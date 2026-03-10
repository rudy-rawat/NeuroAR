using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class QuizUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;        // 4 buttons for A, B, C, D
    public TextMeshProUGUI[] optionTexts; // 4 texts for options
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public Button nextButton;
    public Button closeButton;
    public Button cancelButton;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private QuizQuestion[] selectedQuestions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool answered = false;

    // Agent 1 tracking
    private string _currentOrganType;
    private List<QuizAnswerEntry> _answers = new List<QuizAnswerEntry>();
    private float _questionStartTime;

    // Agent 3
    private QuizService _quizService;
    private bool _initialized = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextQuestion);
            nextButton.gameObject.SetActive(false);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePanel);

        // Setup option buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Capture for closure
            optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        _quizService = gameObject.AddComponent<QuizService>();
    }

    public void StartQuiz(string organType)
    {
        Debug.Log($"StartQuiz called with organType: '{organType}'");

        // Guarantee init has run even if Awake hasn't fired yet
        EnsureInitialized();

        if (string.IsNullOrEmpty(organType))
        {
            Debug.LogWarning("No organ type provided!");
            if (questionText != null)
                questionText.text = "No organ detected. Please scan an organ first.";
            return;
        }

        currentQuestionIndex = 0;
        score = 0;
        _currentOrganType = organType;
        _answers.Clear();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Show loading state immediately so the user never sees a blank template
        ShowLoadingState();

        // Resolve backend URL from whichever manager is available
        string backendUrl = "";
        if (LearnerProfileManager.Instance != null)
            backendUrl = LearnerProfileManager.Instance.azureFunctionsBaseUrl;
        if (string.IsNullOrEmpty(backendUrl) || backendUrl.Contains("your-app"))
            backendUrl = NarrationManager.Instance != null
                ? NarrationManager.Instance.azureFunctionsBaseUrl
                : "https://neuroai-backend-production.up.railway.app";
        _quizService.Init(backendUrl);

        StartCoroutine(RequestAgent3Questions(organType));
    }

    private void ShowLoadingState()
    {
        if (questionText != null)
            questionText.text = "Loading personalised questions...";
        foreach (var btn in optionButtons)
            if (btn != null) btn.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (scoreText != null) scoreText.text = "";
        if (feedbackText != null) feedbackText.text = "";
    }

    private IEnumerator RequestAgent3Questions(string organType)
    {
        // Use Agent 1 data if available, otherwise safe defaults
        string level = "beginner";
        string[] weakConcepts = new string[0];
        string userId = "guest";

        if (LearnerProfileManager.Instance != null && LearnerProfileManager.Instance.IsReady)
        {
            level = LearnerProfileManager.Instance.GetLearningLevel();
            weakConcepts = LearnerProfileManager.Instance.GetWeakConcepts().ToArray();
        }
        if (UserSession.Instance != null && UserSession.Instance.IsLoggedIn)
            userId = UserSession.Instance.UserId;

        var req = new QuizService.QuestionRequest
        {
            userId       = userId,
            organName    = organType,
            level        = level,
            weakConcepts = weakConcepts,
            count        = 5
        };

        yield return _quizService.RequestQuestions(req,
            response =>
            {
                if (response?.questions != null && response.questions.Length > 0)
                {
                    selectedQuestions = QuizService.ToQuizQuestions(response.questions);
                    string src = response.source == "agent" ? "AI" : "fallback";
                    Debug.Log($"[Agent3] Loaded {selectedQuestions.Length} questions (source: {src})");
                }
                else
                {
                    Debug.LogWarning("[Agent3] Empty response from backend.");
                    selectedQuestions = null;
                }
            },
            err =>
            {
                Debug.LogWarning($"[Agent3] Question request failed: {err}");
                selectedQuestions = null;
            }
        );

        // Restore option buttons visibility before showing first question
        foreach (var btn in optionButtons)
            if (btn != null) btn.gameObject.SetActive(true);

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (selectedQuestions == null || selectedQuestions.Length == 0)
        {
            Debug.LogWarning("[QuizUI] No questions available.");
            if (questionText != null)
                questionText.text = "No questions available. Please try again later.";
            foreach (var btn in optionButtons)
                if (btn != null) btn.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            return;
        }

        if (currentQuestionIndex >= selectedQuestions.Length)
        {
            ShowResults();
            return;
        }

        answered = false;
        _questionStartTime = Time.realtimeSinceStartup;
        QuizQuestion question = selectedQuestions[currentQuestionIndex];

        // Display question
        if (questionText != null)
            questionText.text = $"Q{currentQuestionIndex + 1}: {question.question}";

        // Display options
        for (int i = 0; i < optionButtons.Length && i < question.options.Length; i++)
        {
            if (optionTexts[i] != null)
                optionTexts[i].text = question.options[i];

            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = true;
                optionButtons[i].GetComponent<Image>().color = Color.white;
            }
        }

        // Update score display
        if (scoreText != null)
            scoreText.text = $"Score: {score}/{selectedQuestions.Length}";

        // Clear feedback
        if (feedbackText != null)
            feedbackText.text = "";

        // Hide next button
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (answered) return;

        answered = true;
        QuizQuestion question = selectedQuestions[currentQuestionIndex];
        bool correct = question.IsCorrect(selectedIndex);

        if (correct)
        {
            score++;
            if (feedbackText != null)
            {
                feedbackText.text = "[Correct!]";
                feedbackText.color = Color.green;
            }

            // Highlight correct answer in green
            if (optionButtons[selectedIndex] != null)
                optionButtons[selectedIndex].GetComponent<Image>().color = Color.green;
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = "[Wrong!] Correct answer: " + question.options[question.correctAnswerIndex];
                feedbackText.color = Color.red;
            }

            // Highlight wrong answer in red
            if (optionButtons[selectedIndex] != null)
                optionButtons[selectedIndex].GetComponent<Image>().color = Color.red;

            // Show correct answer in green
            if (optionButtons[question.correctAnswerIndex] != null)
                optionButtons[question.correctAnswerIndex].GetComponent<Image>().color = Color.green;
        }

        // Record answer for Agent 1
        _answers.Add(new QuizAnswerEntry
        {
            question         = question.question,
            selectedOption   = question.options[selectedIndex],
            correctOption    = question.options[question.correctAnswerIndex],
            isCorrect        = correct,
            timeSpentSeconds = Mathf.RoundToInt(Time.realtimeSinceStartup - _questionStartTime)
        });

        // Disable all buttons
        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.interactable = false;
        }

        // Update score
        if (scoreText != null)
            scoreText.text = $"Score: {score}/{selectedQuestions.Length}";

        // Show next button
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;
        ShowQuestion();
    }

    private void ShowResults()
    {
        SubmitQuizToAgent1();

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            float percentage = selectedQuestions.Length > 0
                ? (score / (float)selectedQuestions.Length) * 100
                : 0f;
            string grade = GetGrade(percentage);

            if (resultText != null)
            {
                resultText.text = $"Quiz Complete!\n\n" +
                                 $"Score: {score}/{selectedQuestions.Length}\n" +
                                 $"Percentage: {percentage:F0}%\n" +
                                 $"Grade: {grade}";
            }
        }

        // Hide question UI
        if (questionText != null)
            questionText.text = "";

        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
    }

    private string GetGrade(float percentage)
    {
        if (percentage >= 90) return "A+ Excellent!";
        if (percentage >= 80) return "A Good Job!";
        if (percentage >= 70) return "B Well Done!";
        if (percentage >= 60) return "C Fair";
        if (percentage >= 50) return "D Pass";
        return "F Needs Improvement";
    }

    private void SubmitQuizToAgent1()
    {
        if (LearnerProfileManager.Instance == null || !LearnerProfileManager.Instance.IsReady) return;
        if (string.IsNullOrEmpty(_currentOrganType) || selectedQuestions == null) return;

        float pct = (score / (float)selectedQuestions.Length) * 100f;

        var result = new QuizResult
        {
            quizId         = Guid.NewGuid().ToString(),
            organName      = _currentOrganType,
            takenAt        = DateTime.UtcNow.ToString("o"),
            triggeredBy    = "user",
            totalQuestions = selectedQuestions.Length,
            score          = score,
            percentage     = pct,
            answers        = new List<QuizAnswerEntry>(_answers)
        };

        LearnerProfileManager.Instance.SubmitQuizResult(
            result,
            onDone: _ => Debug.Log($"[Agent1] Quiz submitted: {score}/{selectedQuestions.Length} ({pct:F0}%)"),
            onError: err => Debug.LogWarning($"[Agent1] Quiz submit failed: {err}")
        );
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);

        // Reset for next time
        if (resultPanel != null)
            resultPanel.SetActive(false);

        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(true);
        }
    }
}