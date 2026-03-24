# AI Agent Feedback System Architecture

## 1. System Overview

This document outlines the architecture for a safe, dual-path feedback system in an AI-driven application. To prevent **data poisoning** (where users input false or malicious feedback to manipulate the AI), this system strictly separates raw user telemetry from the variables that actively influence the AI agents.

### The Dual-Path Strategy

- **Path A — Global Telemetry:** Raw text and feedback are logged securely to the database. Developers use this to analyze trends and manually update the core AI system prompts. The AI **never** reads this raw text.
- **Path B — Local AI Sandbox:** User UI actions are translated into strict, mathematically bounded variables (e.g., `complexityLevel: -1`). These variables are injected into the AI's prompt at runtime to personalize the experience safely.

---

## 2. Database Schema

Two separate collections isolate the telemetry data from the active AI context.

### A. `UserProfiles` — The AI Sandbox

Stores safe, bounded preferences for each user.

```json
{
  "_id": "user_12345",
  "learningPreferences": {
    "complexityLevel": 5,
    "pacing": "moderate",
    "visualDependency": true
  }
}
```

| Field | Type | Constraints |
|---|---|---|
| `complexityLevel` | Integer | Range: 1–10 |
| `pacing` | String | Enum: `"slow"`, `"moderate"`, `"fast"` |
| `visualDependency` | Boolean | — |

### B. `FeedbackLogs` — Developer Telemetry

Stores raw feedback for developer review and macro-adjustments.

```json
{
  "_id": "log_98765",
  "userId": "user_12345",
  "agentId": "anatomy_tutor_agent",
  "rawFeedbackText": "The explanation of the cardiac cycle was completely wrong and confusing.",
  "suggestedAction": "decrease_complexity",
  "timestamp": "2026-03-24T12:00:00Z"
}
```

---

## 3. Unity Frontend Implementation

The Unity client captures user feedback and sends both the raw text (for developers) and a predefined `suggestedAction` tag (for backend logic). This ensures malicious prompts cannot be executed.

**FeedbackPayload construction:** When a UI button is pressed, the client assembles a payload containing the `userId` (pulled from the active auth session), the `agentId` of the current agent, the raw feedback text typed by the user, and a fixed `suggestedAction` tag mapped to that button (e.g., `"decrease_complexity"`).

**Sending the request:** The payload is serialized to JSON and dispatched as an HTTP POST to the backend feedback endpoint using `UnityWebRequest`. The request runs as a coroutine to avoid blocking the main thread. On failure, the error is logged; on success, a confirmation is logged.

---

## 4. Node.js Backend Implementation

The Express backend acts as a strict firewall. It processes the incoming payload, logs raw text for **Path A**, and mathematically adjusts the user profile for **Path B** using a strict allowlist.

**Path A — Telemetry logging:** Every incoming request is immediately written in full to the `FeedbackLogs` collection — including the raw feedback text, the suggested action, the user ID, agent ID, and a server-generated timestamp. This happens unconditionally, before any profile logic runs.

**Path B — Allowlist validation:** The `suggestedAction` value is checked against a hardcoded allowlist (`decrease_complexity`, `increase_complexity`, `slower_pace`). Any value not on this list is silently ignored — it has no effect on the user profile.

**Path B — Bounded profile update:** For each allowed action, a constrained math operation is applied to the user's profile. For `decrease_complexity`, the `complexityLevel` field is decremented by 1 but clamped so it never falls below 1. This ensures the stored value always remains a safe integer within the defined range, regardless of how many times the action is triggered.

**Error handling:** Any database failure returns a `500` response. Successful processing returns a `200` confirmation.

---

## 5. AI Agent Prompt Construction

When the Unity app requests the next AI interaction, the backend retrieves safe, numerical parameters and dynamically injects them into the system prompt. The AI adapts without ever reading the raw, potentially malicious user feedback.

**Fetching preferences:** The backend looks up the user's `UserProfile` by ID and reads the `learningPreferences` object — specifically `complexityLevel` and `pacing`.

**Building the system prompt:** These values are interpolated into a structured system prompt that instructs the AI to match a specific complexity level out of 10 and a preferred pacing style. The prompt explicitly tells the AI to adjust vocabulary, explanation depth, and sentence structure accordingly, without compromising factual accuracy.

**Calling the LLM:** The constructed system prompt is passed to the LLM API alongside the user's message. Because only sanitized, bounded numbers ever reach this step, the AI has no exposure to raw user input.

---

## 6. Security Summary

| Property | Description |
|---|---|
| **No Direct Prompt Injection** | User text input is entirely isolated from the AI's context window, preventing prompt injection via the feedback loop. |
| **Bounded Impact** | False feedback only affects the specific user's local `learningPreferences` state, preventing cross-user contamination. |
| **Developer Override** | Developers retain full control over core system prompts and can reset any user's local profile if manipulation is detected. |
