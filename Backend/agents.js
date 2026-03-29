import { ChatGroq } from "@langchain/groq";

const model = new ChatGroq({
  apiKey: process.env.GROQ_API_KEY,
  model: process.env.GROQ_MODEL ?? "llama-3.3-70b-versatile",
  temperature: 0.2,
});

const narrationStyles = [
  "story-like and vivid",
  "exam-focused and concise",
  "cause-and-effect explanation",
  "myth-busting with one surprising fact",
  "analogy-first and beginner friendly",
];

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Strip markdown fences then parse JSON, with a regex fallback. */
const parseJSON = (raw) => {
  try {
    return JSON.parse(raw.replace(/```json|```/g, "").trim());
  } catch {
    const match = raw.match(/\{[\s\S]*\}|\[[\s\S]*\]/);
    if (match) return JSON.parse(match[0]);
    throw new Error("Could not parse model JSON response.");
  }
};

const normalizeText = (text = "") =>
  text
    .toLowerCase()
    .replace(/[^a-z0-9\s]/g, " ")
    .replace(/\s+/g, " ")
    .trim();

const tokenSet = (text = "") =>
  new Set(normalizeText(text).split(" ").filter(Boolean));

const isTooSimilarNarration = (candidate, previous = []) => {
  const c = normalizeText(candidate);
  if (!c) return true;

  for (const prev of previous) {
    const p = normalizeText(prev);
    if (!p) continue;
    if (c === p) return true;

    const a = tokenSet(c);
    const b = tokenSet(p);
    if (!a.size || !b.size) continue;

    let overlap = 0;
    for (const token of a) {
      if (b.has(token)) overlap += 1;
    }

    const jaccard = overlap / (a.size + b.size - overlap);
    if (jaccard >= 0.75) return true;
  }

  return false;
};

/**
 * Merge wrong answers from a quiz into the repeatedMistakes list.
 * Each entry: { organName, question, wrongCount, lastWrongAt }
 */
export const mergeRepeatedMistakes = (
  existing = [],
  organName,
  answers = [],
) => {
  const map = new Map(existing.map((m) => [m.question, { ...m }]));
  for (const ans of answers) {
    if (!ans.isCorrect) {
      if (map.has(ans.question)) {
        const entry = map.get(ans.question);
        entry.wrongCount += 1;
        entry.lastWrongAt = new Date().toISOString();
      } else {
        map.set(ans.question, {
          organName,
          question: ans.question,
          wrongCount: 1,
          lastWrongAt: new Date().toISOString(),
        });
      }
    }
  }
  return Array.from(map.values());
};

// ─── Agent 1 : Adaptive Learning Evaluator ───────────────────────────────────
// Recomputes the agentSummary from the full learner profile.
// Called after organ/log and quiz/submit.
//
// params: { userProfile }
// returns: updated agentSummary object

export const computeAgentSummary = async (userProfile) => {
  const { onboarding = {}, organHistory = {}, quizHistory = [] } = userProfile;

  const prompt = `
You are an adaptive learning AI for a biology AR app. Analyse this student's
learning data and return a JSON summary.

Student profile:
- Grade: ${onboarding.grade ?? "unknown"}
- Prior knowledge: ${onboarding.priorKnowledge ?? "unknown"}
- Learning goal: ${onboarding.learningGoal ?? "unknown"}
- Organs studied: ${JSON.stringify(organHistory)}
- Quiz history: ${JSON.stringify(quizHistory.slice(-20))}

Return ONLY valid JSON with this structure:
{
  "overallLevel": "beginner" | "intermediate" | "advanced",
  "weakConcepts": ["..."],
  "strongConcepts": ["..."],
  "recommendedNextOrgan": "..."
}

Rules:
- weakConcepts: anatomy concepts with repeated wrong answers or low quiz scores.
- strongConcepts: concepts the student consistently answers correctly.
- overallLevel: reflects broad anatomy knowledge across all history.
- recommendedNextOrgan: an organ not yet studied or the one with the weakest performance.
- Return ONLY the JSON object — no markdown, no extra text.
`;

  const response = await model.invoke(prompt);
  return parseJSON(response.content);
};

// ─── Agent 2 : AR Narrator ────────────────────────────────────────────────────
// Generates a short personalised narration when a student points at an organ.
// Called by POST /api/agent/narrate.
//
// params: { organName, level, weakConcepts, sessionCount }
// returns: { narrationText }

export const generateNarration = async ({
  organName,
  level,
  weakConcepts = [],
  sessionCount = 0,
  pageIndex = 0,
  recentNarrations = [],
}) => {
  const pages = [
    `Give a short intro (2-3 sentences) about the ${organName}. What is it and what is its main function?`,
    `Explain the structure of the ${organName} in 2-3 sentences. Mention its key parts.`,
    `Describe how the ${organName} works step by step in 2-3 sentences.`,
    `Share an interesting fact or a common misconception about the ${organName} in 2-3 sentences.`,
    `Explain what happens when the ${organName} is diseased or damaged, in 2-3 sentences.`,
  ];
  const focus = pages[pageIndex % pages.length];
  const styleMode =
    narrationStyles[(sessionCount + pageIndex) % narrationStyles.length];
  const avoidLines = recentNarrations
    .slice(-5)
    .map((line, idx) => `${idx + 1}. ${line}`)
    .join("\n");

  const basePrompt = `
You are a friendly biology tutor in an AR anatomy app for Indian school students
(Class 9–12). The student is looking at the ${organName} model.

Rules:
- If level = "beginner": use simple language, no jargon
- If level = "intermediate": use some terminology with brief explanation
- If level = "advanced": use full medical terminology
- If sessionCount > 2: acknowledge they've seen this before
- If weakConcepts is not empty: gently nudge them toward those topics
- Keep it between 45-80 words
- Do NOT say "I am an AI"
- Do NOT repeat the organ name at the start of every sentence
- Use a fresh opening phrase and different sentence rhythm from previous outputs
- Do not reuse any phrase of 6+ words from recent narrations
- Use this style mode for variety: ${styleMode}

Student level: ${level}
Times studied: ${sessionCount}
Weak concepts: ${weakConcepts.length ? weakConcepts.join(", ") : "none"}
Page: ${pageIndex + 1}

Focus for this page: ${focus}

Recent narrations to avoid repeating:
${avoidLines || "none"}

Generate the narration text only, no extra formatting.
`;

  for (let attempt = 0; attempt < 2; attempt += 1) {
    const retryInstruction =
      attempt === 0
        ? ""
        : "\nRetry: previous output was too similar. Use a different angle and wording.\n";
    const response = await model.invoke(`${basePrompt}${retryInstruction}`);
    const narrationText = response.content.trim();

    if (!isTooSimilarNarration(narrationText, recentNarrations.slice(-5))) {
      return { narrationText, pageIndex, styleMode };
    }
  }

  const fallback = `${organName} helps keep the body balanced through linked structure and function. Focus now on ${weakConcepts[0] ?? "core anatomy"} and notice how each part supports the next step in the process.`;
  return { narrationText: fallback, pageIndex, styleMode };
};

// ─── Agent 3 : Quiz Question Generator ───────────────────────────────────────
// Generates targeted MCQ questions for a given organ and student profile.
// Called by POST /api/agent/question.
//
// params: { organName, level, weakConcepts, count }
// returns: { source, questions }

export const generateQuestions = async ({
  organName,
  level,
  weakConcepts = [],
  count = 5,
  previousQuestions = [],
}) => {
  const exclusionBlock = previousQuestions.length
    ? `\nDo NOT repeat or rephrase any of these previously asked questions:\n${previousQuestions.map((q, i) => `${i + 1}. ${q}`).join("\n")}\n`
    : "";

  const prompt = `
You are a biology quiz generator for an AR anatomy app for Indian students (Class 9–12).
Generate MCQ questions that are accurate, challenging at the right level, and focus
on the student's weak areas.

Rules:
- Always return exactly ${count} questions
- Each question must have exactly 4 options (A, B, C, D)
- correctAnswerIndex is 0-based (0=A, 1=B, 2=C, 3=D)
- Prioritise questions about the weakConcepts listed
- Match difficulty to the level: beginner=recall, intermediate=application, advanced=analysis
- Every question must be unique and different from previous quizzes
- Return ONLY valid JSON, no markdown, no extra text
${exclusionBlock}
Organ: ${organName}
Level: ${level}
Weak concepts to target: ${weakConcepts.length ? weakConcepts.join(", ") : "general"}
Number of questions: ${count}

Return JSON in this exact format:
{
  "questions": [
    {
      "question": "...",
      "options": ["...", "...", "...", "..."],
      "correctAnswerIndex": 0,
      "explanation": "Brief explanation of the correct answer."
    }
  ]
}
`;

  try {
    const response = await model.invoke(prompt);
    const parsed = parseJSON(response.content);

    // Validate structure — accept any count >= 1 (LLMs don't always return exactly N)
    if (
      !Array.isArray(parsed.questions) ||
      parsed.questions.length === 0 ||
      parsed.questions.some(
        (q) =>
          !q.question || !Array.isArray(q.options) || q.options.length !== 4,
      )
    ) {
      console.error(
        "[Agent3] Invalid question structure:",
        JSON.stringify(parsed).slice(0, 300),
      );
      return { source: "fallback", questions: [] };
    }

    // Trim to requested count if the model returned too many
    const questions = parsed.questions.slice(0, count);
    return { source: "agent", questions };
  } catch {
    return { source: "fallback", questions: [] };
  }
};

// ─── Agent 4 : Learning Roadmap Generator ────────────────────────────────────
// Generates a step-by-step curriculum roadmap based on student profile.
// Called by GET /api/agent/roadmap/:userId.
// Results are cached in the profile and refreshed only when agentSummary changes significantly.
//
// params: { userProfile }
// returns: { roadmapId, generatedAt, overallFocus, steps }

export const generateRoadmap = async (userProfile) => {
  const { onboarding = {}, agentSummary = {}, organHistory = {} } = userProfile;

  const prompt = `
You are an adaptive learning coach for a biology AR anatomy app for Indian school students (Class 9–12).
Create a structured, step-by-step learning roadmap that helps the student overcome their weak areas
and build a strong foundation in anatomy.

Student Profile:
- Grade: ${onboarding.grade ?? "unknown"}
- Prior knowledge: ${onboarding.priorKnowledge ?? "unknown"}
- Learning goal: ${onboarding.learningGoal ?? "unknown"}
- Overall level: ${agentSummary.overallLevel ?? "beginner"}
- Organs already studied: ${Object.keys(organHistory).join(", ") || "none"}
- Weak concepts: ${agentSummary.weakConcepts?.join(", ") || "none"}
- Strong concepts: ${agentSummary.strongConcepts?.join(", ") || "none"}
- Recommended next organ: ${agentSummary.recommendedNextOrgan ?? "none"}

Return ONLY valid JSON with this exact structure. No markdown, no extra text:
{
  "overallFocus": "A brief focus area (1 sentence) based on their weak concepts",
  "steps": [
    {
      "stepNumber": 1,
      "topic": "Clear, concise topic name",
      "description": "2-3 sentence description of what to learn and why it matters",
      "estimatedTimeInMinutes": 15,
      "resources": [
        {
          "name": "Resource name",
          "type": "organ_scene|external_link",
          "referenceId": "organ_name (if organ_scene)",
          "url": "https://... (if external_link)"
        }
      ]
    }
  ]
}

Rules:
- Generate 3-5 steps in a logical progression
- Each step should take 10-25 minutes
- Prioritise fixing weak concepts and building on strong ones
- Resource types:
  * "organ_scene": use referenceId with organ names like "heart", "brain", "kidney", "liver", "lungs", "stomach"
  * "external_link": use url with a real educational resource (YouTube, Khan Academy-style)
- For beginner level: focus on basic anatomy and structure
- For intermediate: combination of structure and function
- For advanced: include pathology and clinical relevance
- Always start with the recommended next organ or a weak concept
- Ensure smooth progression from foundational to more advanced topics
- Return ONLY the JSON object — no explanations, no markdown.
`;

  const response = await model.invoke(prompt);
  return parseJSON(response.content);
};
