import { MongoClient } from "mongodb";
import dotenv from "dotenv";

dotenv.config();

const client = new MongoClient(process.env.MONGODB_URI);
await client.connect();

const db = client.db(process.env.MONGODB_DB_NAME);
export const usersCollection = db.collection(
  process.env.MONGODB_COLLECTION_USERS,
);
export const feedbackLogsCollection = db.collection(
  process.env.MONGODB_COLLECTION_FEEDBACK_LOGS ?? "feedbackLogs",
);

const ALLOWED_PACING = new Set(["slow", "moderate", "fast"]);

export const DEFAULT_LEARNING_PREFERENCES = {
  complexityLevel: 5,
  pacing: "moderate",
  visualDependency: false,
};

const clampComplexity = (value) => {
  const parsed = Number.parseInt(value, 10);
  if (Number.isNaN(parsed)) return DEFAULT_LEARNING_PREFERENCES.complexityLevel;
  return Math.min(10, Math.max(1, parsed));
};

export const normalizeLearningPreferences = (prefs = {}) => ({
  complexityLevel: clampComplexity(prefs.complexityLevel),
  pacing: ALLOWED_PACING.has(prefs.pacing)
    ? prefs.pacing
    : DEFAULT_LEARNING_PREFERENCES.pacing,
  visualDependency:
    typeof prefs.visualDependency === "boolean"
      ? prefs.visualDependency
      : DEFAULT_LEARNING_PREFERENCES.visualDependency,
});

export const ensureLearningPreferences = async (userId) => {
  const profile = await getUserProfile(userId);
  if (!profile) throw new Error("User not found");

  const normalized = normalizeLearningPreferences(profile.learningPreferences);
  const alreadyPresent =
    profile.learningPreferences &&
    profile.learningPreferences.complexityLevel ===
      normalized.complexityLevel &&
    profile.learningPreferences.pacing === normalized.pacing &&
    profile.learningPreferences.visualDependency ===
      normalized.visualDependency;

  if (alreadyPresent) {
    return normalized;
  }

  const updated = {
    ...profile,
    learningPreferences: normalized,
    lastActiveAt: new Date().toISOString(),
  };
  await updateUserProfile(updated);
  return normalized;
};

export const logFeedback = async ({
  userId,
  agentId,
  rawFeedbackText,
  suggestedAction,
  metadata,
}) => {
  const now = new Date();
  const result = await feedbackLogsCollection.insertOne({
    userId,
    agentId,
    rawFeedbackText,
    suggestedAction,
    metadata: metadata ?? {},
    timestamp: now.toISOString(),
    createdAt: now,
  });
  return result.insertedId;
};

export const applyFeedbackActionToPreferences = async (
  userId,
  suggestedAction,
) => {
  const profile = await getUserProfile(userId);
  if (!profile) throw new Error("User not found");

  const prefs = normalizeLearningPreferences(profile.learningPreferences);
  const next = { ...prefs };

  switch (suggestedAction) {
    case "decrease_complexity":
      next.complexityLevel = Math.max(1, prefs.complexityLevel - 1);
      break;
    case "increase_complexity":
      next.complexityLevel = Math.min(10, prefs.complexityLevel + 1);
      break;
    case "slower_pace":
      next.pacing = "slow";
      break;
    case "faster_pace":
      next.pacing = "fast";
      break;
    case "set_pace_moderate":
      next.pacing = "moderate";
      break;
    case "enable_visual_dependency":
      next.visualDependency = true;
      break;
    case "disable_visual_dependency":
      next.visualDependency = false;
      break;
    default:
      // Unknown actions are ignored by design to prevent unbounded influence.
      return prefs;
  }

  const updated = {
    ...profile,
    learningPreferences: next,
    lastActiveAt: new Date().toISOString(),
  };
  await updateUserProfile(updated);
  return next;
};

export const getFeedbackLogsForUser = async (userId, limit = 50) => {
  const safeLimit = Math.min(
    Math.max(Number.parseInt(limit, 10) || 50, 1),
    200,
  );
  return feedbackLogsCollection
    .find({ userId })
    .sort({ createdAt: -1 })
    .limit(safeLimit)
    .project({
      _id: 1,
      userId: 1,
      agentId: 1,
      rawFeedbackText: 1,
      suggestedAction: 1,
      metadata: 1,
      timestamp: 1,
    })
    .toArray();
};
export const getUserProfile = async (userId) => {
  const doc = await usersCollection.findOne({ _id: userId });
  return doc ?? null;
};

export const updateUserProfile = async (userDoc) => {
  const _id = userDoc.id ?? userDoc.userId;
  const doc = { ...userDoc, _id };
  await usersCollection.replaceOne({ _id }, doc, { upsert: true });
  return doc;
};

/**
 * Create a brand-new user profile document.
 * `profileData` should include at minimum: id, userId, displayName, email, onboarding.
 */
export const createUserProfile = async (profileData) => {
  const now = new Date().toISOString();
  const doc = {
    ...profileData,
    _id: profileData.userId,
    id: profileData.userId,
    createdAt: now,
    lastActiveAt: now,
    organHistory: {},
    quizHistory: [],
    narrationHistory: {},
    learningPreferences: normalizeLearningPreferences(
      profileData.learningPreferences,
    ),
    agentSummary: {
      lastComputedAt: now,
      organsStudiedCount: 0,
      strongConcepts: [],
      weakConcepts: [],
      repeatedMistakes: [],
      recommendedNextOrgan: null,
      overallLevel: profileData.onboarding?.priorKnowledge ?? "beginner",
    },
  };
  await usersCollection.insertOne(doc);
  return doc;
};

/**
 * Create a blank profile for a first-time user (called by POST /api/profile/load).
 * Only requires userId — all other fields are set to empty defaults.
 */
export const createBlankProfile = async (
  userId,
  { displayName = "", email = "", photoUrl = "" } = {},
) => {
  const now = new Date().toISOString();
  const doc = {
    _id: userId,
    id: userId,
    userId,
    displayName,
    email,
    photoUrl,
    isGuest: false,
    createdAt: now,
    lastActiveAt: now,
    onboarding: { completed: false },
    organHistory: {},
    quizHistory: [],
    narrationHistory: {},
    learningPreferences: { ...DEFAULT_LEARNING_PREFERENCES },
    agentSummary: {
      lastComputedAt: now,
      organsStudiedCount: 0,
      strongConcepts: [],
      weakConcepts: [],
      repeatedMistakes: [],
      recommendedNextOrgan: null,
      overallLevel: "beginner",
    },
    roadmap: null,
  };
  await usersCollection.insertOne(doc);
  return doc;
};

/**
 * Shallow-merge `patch` into a specific organ's history entry.
 * Patch can include: viewedBasic, viewedDetailed, viewedLabels, viewedInfo,
 *                    totalTimeSeconds (will be incremented), etc.
 */
export const updateOrganHistory = async (userId, organName, patch = {}) => {
  const profile = await getUserProfile(userId);
  if (!profile) throw new Error("User not found");

  const now = new Date().toISOString();
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

  const updated = {
    ...profile,
    lastActiveAt: now,
    organHistory: {
      ...profile.organHistory,
      [organName]: {
        ...existing,
        lastStudiedAt: now,
        // Increment totalTimeSeconds if provided, otherwise keep existing
        totalTimeSeconds:
          existing.totalTimeSeconds + (patch.addTimeSeconds ?? 0),
        ...Object.fromEntries(
          Object.entries(patch).filter(([k]) => k !== "addTimeSeconds"),
        ),
      },
    },
  };

  return updateUserProfile(updated);
};

/**
 * Save a generated roadmap to the user's profile.
 * The roadmap is cached and will only be regenerated if agentSummary changes significantly.
 *
 * params: { userId, roadmapData }
 * returns: updated profile with roadmap
 */
export const updateRoadmap = async (userId, roadmapData) => {
  const profile = await getUserProfile(userId);
  if (!profile) throw new Error("User not found");

  const updated = {
    ...profile,
    roadmap: {
      ...roadmapData,
      generatedAt: new Date().toISOString(),
    },
    lastActiveAt: new Date().toISOString(),
  };

  return updateUserProfile(updated);
};

/**
 * Append generated narration to per-organ history, capped to last 12 entries.
 */
export const appendNarrationHistory = async (
  userId,
  organName,
  narrationText,
  pageIndex = 0,
) => {
  const profile = await getUserProfile(userId);
  if (!profile) throw new Error("User not found");

  const now = new Date().toISOString();
  const existingForOrgan = profile.narrationHistory?.[organName] ?? [];

  const nextForOrgan = [
    ...existingForOrgan,
    { text: narrationText, pageIndex, createdAt: now },
  ].slice(-12);

  const updated = {
    ...profile,
    narrationHistory: {
      ...(profile.narrationHistory ?? {}),
      [organName]: nextForOrgan,
    },
    lastActiveAt: now,
  };

  return updateUserProfile(updated);
};
