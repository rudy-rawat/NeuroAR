import dotenv from "dotenv";
dotenv.config();

import express from "express";
import cors from "cors";
import {
  getUserProfile,
  updateUserProfile,
  createBlankProfile,
  updateRoadmap,
  appendNarrationHistory,
  ensureLearningPreferences,
  applyFeedbackActionToPreferences,
  getFeedbackLogsForUser,
  logFeedback,
} from "./db.js";
import {
  computeAgentSummary,
  generateNarration,
  generateQuestions,
  generateRoadmap,
  mergeRepeatedMistakes,
} from "./agents.js";
import { ChatGroq } from "@langchain/groq";

const app = express();
app.use(express.json());
app.use(cors());

const ALLOWED_FEEDBACK_ACTIONS = new Set([
  "decrease_complexity",
  "increase_complexity",
  "slower_pace",
  "faster_pace",
  "set_pace_moderate",
  "enable_visual_dependency",
  "disable_visual_dependency",
]);

// ─── Health check ─────────────────────────────────────────────────────────────
// Open this URL in a browser to confirm the server is running: GET /health
app.get("/health", (_req, res) => res.json({ status: "ok" }));

// ─── GET /api/profile/:userId ─────────────────────────────────────────────────
// Returns the learner profile for the given userId, or 404 if not found.
app.get("/api/profile/:userId", async (req, res) => {
  try {
    const { userId } = req.params;
    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "Profile not found" });

    res.json(profile);
  } catch (error) {
    console.error("GET profile error:", error);
    res.status(500).json({ error: "Failed to get profile." });
  }
});

// ─── GET /api/profile/:userId/learning-preferences ───────────────────────────
// Returns the safe, bounded variables used for runtime personalization.
app.get("/api/profile/:userId/learning-preferences", async (req, res) => {
  try {
    const { userId } = req.params;
    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "Profile not found" });

    const learningPreferences = await ensureLearningPreferences(userId);
    res.json({ userId, learningPreferences });
  } catch (error) {
    console.error("GET learning-preferences error:", error);
    res.status(500).json({ error: "Failed to get learning preferences." });
  }
});

const model = new ChatGroq({
  apiKey: process.env.GROQ_API_KEY,
  model: process.env.GROQ_MODEL ?? "llama-3.3-70b-versatile",
  temperature: 0.3,
});

// ─── 1. POST /api/profile/load ────────────────────────────────────────────────
// Called by Unity on login.
// Loads existing profile or creates a blank one for first-time users.
app.post("/api/profile/load", async (req, res) => {
  try {
    const { userId, displayName, email, photoUrl } = req.body;
    if (!userId) return res.status(400).json({ error: "Missing userId" });

    let profile = await getUserProfile(userId);
    if (!profile) {
      profile = await createBlankProfile(userId, {
        displayName,
        email,
        photoUrl,
      });
    } else {
      // Always backfill any missing identity fields from the client
      const needsUpdate =
        (displayName && !profile.displayName) ||
        (email && !profile.email) ||
        (photoUrl && !profile.photoUrl);
      if (needsUpdate) {
        profile = await updateUserProfile({
          ...profile,
          displayName: displayName || profile.displayName || "",
          email: email || profile.email || "",
          photoUrl: photoUrl || profile.photoUrl || "",
        });
      }
    }

    res.json(profile);
  } catch (error) {
    console.error("profile/load error:", error);
    res.status(500).json({ error: "Failed to load profile." });
  }
});

// ─── 2. POST /api/profile/save ────────────────────────────────────────────────
// Called by Unity after onboarding completes.
// Upserts the full profile document.
app.post("/api/profile/save", async (req, res) => {
  try {
    const profile = req.body;
    if (!profile?.userId) {
      return res.status(400).json({ error: "Missing userId in profile." });
    }

    const saved = await updateUserProfile({
      ...profile,
      id: profile.userId,
      lastActiveAt: new Date().toISOString(),
    });

    res.json(saved);
  } catch (error) {
    console.error("profile/save error:", error);
    res.status(500).json({ error: "Failed to save profile." });
  }
});

// ─── POST /api/feedback ───────────────────────────────────────────────────────
// Dual-path feedback endpoint:
// Path A: store raw telemetry in FeedbackLogs.
// Path B: map allowlisted actions to bounded preference updates per user.
app.post("/api/feedback", async (req, res) => {
  try {
    const {
      userId,
      agentId = "unknown_agent",
      rawFeedbackText = "",
      suggestedAction,
      metadata,
    } = req.body ?? {};

    if (!userId || !suggestedAction) {
      return res
        .status(400)
        .json({ error: "Missing required fields: userId, suggestedAction." });
    }

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    const trimmedFeedback = String(rawFeedbackText).trim().slice(0, 4000);
    const normalizedAction = String(suggestedAction).trim();

    await logFeedback({
      userId,
      agentId,
      rawFeedbackText: trimmedFeedback,
      suggestedAction: normalizedAction,
      metadata,
    });

    let updatedPreferences = await ensureLearningPreferences(userId);
    if (ALLOWED_FEEDBACK_ACTIONS.has(normalizedAction)) {
      updatedPreferences = await applyFeedbackActionToPreferences(
        userId,
        normalizedAction,
      );
    }

    res.status(200).json({
      message: "Feedback processed securely",
      appliedAction: ALLOWED_FEEDBACK_ACTIONS.has(normalizedAction)
        ? normalizedAction
        : null,
      learningPreferences: updatedPreferences,
    });
  } catch (error) {
    console.error("feedback processing error:", error);
    res.status(500).json({ error: "Failed to process feedback." });
  }
});

// ─── GET /api/feedback/logs/:userId ───────────────────────────────────────────
// Developer telemetry endpoint for raw feedback review.
app.get("/api/feedback/logs/:userId", async (req, res) => {
  try {
    const { userId } = req.params;
    const { limit } = req.query;

    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const logs = await getFeedbackLogsForUser(userId, limit);
    res.json({ userId, count: logs.length, logs });
  } catch (error) {
    console.error("GET feedback logs error:", error);
    res.status(500).json({ error: "Failed to fetch feedback logs." });
  }
});

// ─── GET /api/agent/prompt-context/:userId ───────────────────────────────────
// Returns a safe prompt scaffold built only from bounded preferences.
app.get("/api/agent/prompt-context/:userId", async (req, res) => {
  try {
    const { userId } = req.params;
    const topic = String(req.query.topic ?? "general anatomy").slice(0, 120);

    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    const prefs = await ensureLearningPreferences(userId);
    const systemPrompt = [
      "You are an expert AI learning agent.",
      "",
      `Current Topic: ${topic}`,
      "",
      "USER SPECIFIC CONSTRAINTS:",
      `- Target Complexity Level: ${prefs.complexityLevel} out of 10.`,
      `- Preferred Pacing: ${prefs.pacing}.`,
      `- Visual Dependency: ${prefs.visualDependency ? "high" : "low"}.`,
      "",
      "INSTRUCTIONS: Adjust vocabulary, depth, and sentence length to match",
      "these constraints while preserving factual accuracy.",
    ].join("\n");

    res.json({
      userId,
      topic,
      learningPreferences: prefs,
      systemPrompt,
    });
  } catch (error) {
    console.error("GET prompt-context error:", error);
    res.status(500).json({ error: "Failed to build prompt context." });
  }
});

// ─── 3. POST /api/organ/log ───────────────────────────────────────────────────
// Called by Unity when an AR organ target is lost (session ends).
// Records organ session data and recomputes agentSummary via GPT-4o (Agent 1).
//
// Body: { userId, organName, addTimeSeconds?, viewedBasic?, viewedDetailed?,
//         viewedLabels?, viewedInfo? }
app.post("/api/organ/log", async (req, res) => {
  try {
    const { userId, organName, ...patch } = req.body;
    if (!userId || !organName) {
      return res.status(400).json({ error: "Missing userId or organName." });
    }

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    const now = new Date().toISOString();
    const { addTimeSeconds = 0, ...booleanFlags } = patch;

    // Update organHistory
    const existing = profile.organHistory?.[organName] ?? {
      organName,
      firstStudiedAt: now,
      totalTimeSeconds: 0,
      viewedBasic: false,
      viewedDetailed: false,
      viewedLabels: false,
      viewedInfo: false,
      sessionCount: 0,
    };

    const updatedOrganHistory = {
      ...profile.organHistory,
      [organName]: {
        ...existing,
        lastStudiedAt: now,
        sessionCount: existing.sessionCount + 1,
        totalTimeSeconds: existing.totalTimeSeconds + addTimeSeconds,
        ...booleanFlags,
      },
    };

    const profileWithOrgan = {
      ...profile,
      organHistory: updatedOrganHistory,
    };

    // Agent 1: recompute agentSummary
    const evaluated = await computeAgentSummary(profileWithOrgan);

    const updatedProfile = {
      ...profileWithOrgan,
      lastActiveAt: now,
      agentSummary: {
        ...profile.agentSummary,
        overallLevel: evaluated.overallLevel,
        weakConcepts: evaluated.weakConcepts,
        strongConcepts: evaluated.strongConcepts,
        recommendedNextOrgan: evaluated.recommendedNextOrgan,
        organsStudiedCount: Object.keys(updatedOrganHistory).length,
        lastComputedAt: now,
      },
    };

    await updateUserProfile(updatedProfile);
    res.json(updatedProfile);
  } catch (error) {
    console.error("organ/log error:", error);
    res.status(500).json({ error: "Failed to log organ session." });
  }
});

// ─── 4. POST /api/quiz/submit ─────────────────────────────────────────────────
// Called by Unity when a quiz finishes.
// Saves quiz result, refreshes agentSummary (Agent 1), and optionally generates
// GPT-4o explanations for wrong answers.
//
// Body: { userId, organName, quizId?, score, totalQuestions, percentage?,
//         triggeredBy?, answers: [...] }
app.post("/api/quiz/submit", async (req, res) => {
  try {
    // Unity sends { userId, result: { organName, score, answers, ... } }
    // Unwrap the nested result object so existing logic works unchanged.
    const raw = req.body.result
      ? { userId: req.body.userId, ...req.body.result }
      : req.body;

    const { userId, organName, answers = [], ...quizMeta } = raw;
    if (!userId || !organName) {
      return res.status(400).json({ error: "Missing userId or organName." });
    }

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    const now = new Date().toISOString();

    // Append quiz result to quizHistory
    const quizEntry = {
      ...quizMeta,
      organName,
      answers,
      takenAt: now,
      percentage:
        quizMeta.percentage ??
        Math.round((quizMeta.score / quizMeta.totalQuestions) * 100),
    };

    const updatedQuizHistory = [...(profile.quizHistory ?? []), quizEntry];

    // Merge repeated mistakes
    const updatedMistakes = mergeRepeatedMistakes(
      profile.agentSummary?.repeatedMistakes ?? [],
      organName,
      answers,
    );

    const profileWithQuiz = {
      ...profile,
      quizHistory: updatedQuizHistory,
    };

    // Agent 1: refresh agentSummary with the new quiz data
    const evaluated = await computeAgentSummary(profileWithQuiz);

    const updatedProfile = {
      ...profileWithQuiz,
      lastActiveAt: now,
      agentSummary: {
        ...profile.agentSummary,
        overallLevel: evaluated.overallLevel,
        weakConcepts: evaluated.weakConcepts,
        strongConcepts: evaluated.strongConcepts,
        recommendedNextOrgan: evaluated.recommendedNextOrgan,
        repeatedMistakes: updatedMistakes,
        organsStudiedCount: Object.keys(profile.organHistory ?? {}).length,
        lastComputedAt: now,
      },
    };

    await updateUserProfile(updatedProfile);

    // Optional: generate explanations for wrong answers
    const wrongAnswers = answers.filter((a) => !a.isCorrect);
    let explanations = [];

    if (wrongAnswers.length > 0) {
      explanations = await Promise.all(
        wrongAnswers.map(async (ans) => {
          const prompt = `A biology student answered a question incorrectly in an AR anatomy app.

Question: ${ans.question}
Their answer: ${ans.selectedOption}
Correct answer: ${ans.correctOption}
Organ: ${organName}

Give a clear, friendly 1-2 sentence explanation of why the correct answer is right.
No markdown, no labels — just the explanation text.`;

          const response = await model.invoke(prompt);
          return {
            question: ans.question,
            correctOption: ans.correctOption,
            explanation: response.content.trim(),
          };
        }),
      );
    }

    res.json({ updatedProfile, explanations });
  } catch (error) {
    console.error("quiz/submit error:", error);
    res.status(500).json({ error: "Failed to submit quiz." });
  }
});

// ─── 5. POST /api/agent/narrate ───────────────────────────────────────────────
// Called by Unity when an organ is detected (Vuforia TRACKED).
// Generates a personalised narration line via GPT-4o (Agent 2).
//
// Body: { userId, organName, level, weakConcepts?, sessionCount }
app.post("/api/agent/narrate", async (req, res) => {
  try {
    const {
      userId,
      organName,
      level,
      weakConcepts = [],
      sessionCount = 0,
      pageIndex = 0,
    } = req.body;
    if (!organName || !level) {
      return res.status(400).json({ error: "Missing organName or level." });
    }

    let recentNarrations = [];
    let resolvedWeakConcepts = weakConcepts;

    if (userId) {
      const profile = await getUserProfile(userId);
      if (profile) {
        recentNarrations = (profile.narrationHistory?.[organName] ?? [])
          .slice(-5)
          .map((entry) => entry.text)
          .filter(Boolean);

        if (!resolvedWeakConcepts.length) {
          resolvedWeakConcepts = profile.agentSummary?.weakConcepts ?? [];
        }
      }
    }

    const result = await generateNarration({
      organName,
      level,
      weakConcepts: resolvedWeakConcepts,
      sessionCount,
      pageIndex,
      recentNarrations,
    });

    if (userId && result?.narrationText) {
      await appendNarrationHistory(
        userId,
        organName,
        result.narrationText,
        pageIndex,
      );
    }

    res.json(result);
  } catch (error) {
    console.error("agent/narrate error:", error);
    res.status(500).json({ error: "Failed to generate narration." });
  }
});

// ─── 6. POST /api/agent/question ─────────────────────────────────────────────
// Called by Unity when a quiz starts.
// Generates targeted MCQ questions via GPT-4o (Agent 3).
//
// Body: { userId, organName, level, weakConcepts?, count }
app.post("/api/agent/question", async (req, res) => {
  try {
    const { userId, organName, level, weakConcepts = [], count = 5 } = req.body;
    if (!organName || !level) {
      return res.status(400).json({ error: "Missing organName or level." });
    }

    // Fetch recent quiz questions for this organ so Agent 3 can avoid repeats
    let previousQuestions = [];
    if (userId) {
      const profile = await getUserProfile(userId);
      if (profile?.quizHistory) {
        previousQuestions = profile.quizHistory
          .filter((q) => q.organName === organName)
          .flatMap((q) => (q.answers ?? []).map((a) => a.question))
          .filter(Boolean);
        // Keep only the last 30 to stay within prompt limits
        previousQuestions = [...new Set(previousQuestions)].slice(-30);
      }
    }

    const result = await generateQuestions({
      organName,
      level,
      weakConcepts,
      count,
      previousQuestions,
    });

    res.json(result);
  } catch (error) {
    console.error("agent/question error:", error);
    res.status(500).json({ error: "Failed to generate questions." });
  }
});

// ─── 7. GET /api/agent/roadmap/:userId ────────────────────────────────────────
// Called by the client to generate a fresh learning roadmap for the user.
// Always generates a new roadmap via Agent 4 and then caches it.
app.get("/api/agent/roadmap/:userId", async (req, res) => {
  try {
    const { userId } = req.params;

    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    // Generate new roadmap via Agent 4
    const roadmapData = await generateRoadmap(profile);

    // Validate the roadmap structure
    if (
      !roadmapData.overallFocus ||
      !Array.isArray(roadmapData.steps) ||
      roadmapData.steps.length === 0
    ) {
      return res
        .status(500)
        .json({ error: "Failed to generate valid roadmap." });
    }

    // Create the full roadmap object with metadata
    const fullRoadmap = {
      roadmapId: `rm_${Date.now()}_${Math.random().toString(36).substring(7)}`,
      generatedAt: new Date().toISOString(),
      overallFocus: roadmapData.overallFocus,
      steps: roadmapData.steps,
    };

    // Cache the roadmap in the profile
    await updateRoadmap(userId, fullRoadmap);

    res.json(fullRoadmap);
  } catch (error) {
    console.error("agent/roadmap error:", error);
    res.status(500).json({ error: "Failed to fetch or generate roadmap." });
  }
});

// ─── 8. GET /api/agent/roadmap-existing-or-generate/:userId ─────────────────
// Returns the user's existing roadmap if available.
// If no roadmap exists, generates one via Agent 4, stores it, and returns it.
// This route does not change the behavior of /api/agent/roadmap/:userId.
app.get("/api/agent/roadmap-existing-or-generate/:userId", async (req, res) => {
  try {
    const { userId } = req.params;
    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const profile = await getUserProfile(userId);
    if (!profile) return res.status(404).json({ error: "User not found." });

    if (profile.roadmap) {
      return res.json({ source: "existing", roadmap: profile.roadmap });
    }

    const roadmapData = await generateRoadmap(profile);

    if (
      !roadmapData.overallFocus ||
      !Array.isArray(roadmapData.steps) ||
      roadmapData.steps.length === 0
    ) {
      return res
        .status(500)
        .json({ error: "Failed to generate valid roadmap." });
    }

    const fullRoadmap = {
      roadmapId: `rm_${Date.now()}_${Math.random().toString(36).substring(7)}`,
      generatedAt: new Date().toISOString(),
      overallFocus: roadmapData.overallFocus,
      steps: roadmapData.steps,
    };

    await updateRoadmap(userId, fullRoadmap);

    res.json({ source: "generated", roadmap: fullRoadmap });
  } catch (error) {
    console.error("agent/roadmap-existing-or-generate error:", error);
    res
      .status(500)
      .json({ error: "Failed to fetch existing or generate roadmap." });
  }
});

const PORT = process.env.PORT || 8080;
app.listen(PORT, () => {
  console.log(`NeuroAR API running on port ${PORT}`);
});
