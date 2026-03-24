using System;
using System.Collections.Generic;

/// <summary>
/// Root document stored in Cosmos DB, one per student, keyed by userId.
/// Matches the JSON schema sent to / received from the Azure Functions API.
/// </summary>
[Serializable]
public class LearnerProfile
{
    // ── Identity ─────────────────────────────────────────────────────
    public string id;               // same as userId (Cosmos DB requires 'id')
    public string userId;           // Firebase UID
    public string displayName;
    public string email;
    public string photoUrl;
    public bool   isGuest;

    // ── Timestamps ───────────────────────────────────────────────────
    public string createdAt;        // ISO-8601
    public string lastActiveAt;

    // ── Onboarding ───────────────────────────────────────────────────
    public OnboardingData onboarding = new OnboardingData();

    // ── Per-organ history ─────────────────────────────────────────────
    // key = organName (lowercase), e.g. "heart"
    public Dictionary<string, OrganHistoryEntry> organHistory
        = new Dictionary<string, OrganHistoryEntry>();

    // ── Quiz history (chronological) ──────────────────────────────────
    public List<QuizResult> quizHistory = new List<QuizResult>();

    // ── Agent 1 summary (computed by backend) ────────────────────────
    public AgentSummary agentSummary = new AgentSummary();

    // ── Safe feedback sandbox state ───────────────────────────────────
    public LearningPreferences learningPreferences = new LearningPreferences();
}

// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class OnboardingData
{
    public bool   completed      = false;
    public string grade          = "";   // e.g. "Class 11"
    public string priorKnowledge = "";   // "beginner" | "intermediate" | "advanced"
    public string learningGoal   = "";   // "exam preparation" | "curiosity" | "revision"
}

// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class OrganHistoryEntry
{
    public string organName;
    public string firstStudiedAt;
    public string lastStudiedAt;
    public int    totalTimeSeconds;
    public bool   viewedBasic;
    public bool   viewedDetailed;
    public bool   viewedLabels;
    public bool   viewedInfo;
    public int    sessionCount;
}

// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class QuizResult
{
    public string       quizId;
    public string       organName;
    public string       takenAt;
    public string       triggeredBy;     // "user" | "agent"
    public int          totalQuestions;
    public int          score;
    public float        percentage;
    public List<QuizAnswerEntry> answers = new List<QuizAnswerEntry>();
}

[Serializable]
public class QuizAnswerEntry
{
    public string question;
    public string selectedOption;
    public string correctOption;
    public bool   isCorrect;
    public int    timeSpentSeconds;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Computed by Agent 1 on the backend and returned to Unity.
/// Unity stores this in LearnerProfileManager and passes it to Agents 2 & 3.
/// </summary>
[Serializable]
public class AgentSummary
{
    public string       lastComputedAt       = "";
    public int          organsStudiedCount   = 0;
    public List<string> strongConcepts       = new List<string>();
    public List<string> weakConcepts         = new List<string>();
    public List<RepeatedMistake> repeatedMistakes = new List<RepeatedMistake>();
    public string       recommendedNextOrgan = "";
    public string       overallLevel         = "beginner"; // "beginner"|"intermediate"|"advanced"
}

[Serializable]
public class RepeatedMistake
{
    public string organName;
    public string question;
    public int    wrongCount;
    public string lastWrongAt;
}

[Serializable]
public class LearningPreferences
{
    public int complexityLevel = 5;
    public string pacing = "moderate"; // slow | moderate | fast
    public bool visualDependency = false;
}
