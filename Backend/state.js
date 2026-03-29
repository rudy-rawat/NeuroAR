import { Annotation } from "@langchain/langgraph";

/**
 * LangGraph state for the NeuroAI study app.
 *
 * userProfile  – full CosmosDB user document (onboarding, organHistory,
 *                quizHistory, agentSummary, …)
 * action       – routing key: "evaluate_quiz" | "study" | "take_quiz"
 * lastInput    – request payload coming from the frontend
 *                  evaluate_quiz  → { organName, quizResults: [...] }
 *                  study          → { organName }
 *                  take_quiz      → { organName }
 * agentOutput  – final text / JSON string sent back to the client
 */
export const StudyState = Annotation.Root({
  userProfile: Annotation({
    reducer: (x, y) => ({ ...x, ...y }),
    default: () => ({}),
  }),
  action: Annotation({
    reducer: (_x, y) => y,
    default: () => "",
  }),
  lastInput: Annotation({
    reducer: (_x, y) => y,
    default: () => ({}),
  }),
  agentOutput: Annotation({
    reducer: (_x, y) => y,
    default: () => "",
  }),
});
