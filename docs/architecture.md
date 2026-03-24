# AR-Anatomy Architecture

This document explains the end-to-end architecture of AR-Anatomy across Unity client and Node.js backend, including the 4 AI agents.

## 1. High-Level System Architecture

```mermaid
graph TD
    U[Student on Android/Desktop] --> L[Login Scene]
    L --> S[Start Scene]
    S --> M[3D Anatomy Model Scene]
    S --> R[Roadmap Scene]

    subgraph Unity Client
      US[UserSession]
      PM[LearnerProfileManager<br/>Agent 1 Client Orchestrator]
      NM[NarrationManager<br/>Agent 2 Client]
      QU[QuizUI + QuizService<br/>Agent 3 Client]
      RS[RoadmapUI + RoadmapService<br/>Agent 4 Client]
      FS[FeedbackService]
    end

    L --> US
    US --> PM
    M --> NM
    M --> QU
    R --> RS
    S --> FS

    PM -->|POST /api/profile/load| API
    PM -->|POST /api/organ/log| API
    PM -->|POST /api/quiz/submit| API
    NM -->|POST /api/agent/narrate| API
    QU -->|POST /api/agent/question| API
    RS -->|GET /api/agent/roadmap-existing-or-generate/:userId| API
    RS -->|GET /api/agent/roadmap/:userId (refresh)| API
    FS -->|POST /api/feedback| API

    subgraph Backend (Express)
      API[server.js Routes]
      A1[Agent 1<br/>computeAgentSummary]
      A2[Agent 2<br/>generateNarration]
      A3[Agent 3<br/>generateQuestions]
      A4[Agent 4<br/>generateRoadmap]
      DB[MongoDB<br/>users + feedbackLogs]
      LLM[Groq LLM via LangChain]
    end

    API --> A1
    API --> A2
    API --> A3
    API --> A4

    A1 --> LLM
    A2 --> LLM
    A3 --> LLM
    A4 --> LLM

    API --> DB
```

## 2. Unity Architecture

### Core runtime services

- User identity and session:
  - `UserSession` keeps current user id and profile identity.
- Learning profile state:
  - `LearnerProfileManager` loads profile, logs organ sessions, submits quiz results, and exposes Agent 1 summary to the rest of the app.
- Narration flow:
  - `NarrationManager` builds context from `LearnerProfileManager` and calls backend narration endpoint.
- Quiz flow:
  - `QuizUI` requests generated MCQs from backend and later submits quiz outcomes to update profile and summary.
- Roadmap flow:
  - `RoadmapUI` + `RoadmapService` loads existing roadmap when available and supports explicit refresh regeneration.
- Feedback flow:
  - `FeedbackService` sends user feedback actions to backend preference controls.

### Scene responsibilities

- Login Scene:
  - Auth and bootstrap of session/profile.
- Start Scene:
  - Navigation hub and feedback entry point.
- 3D Anatomy Model Scene:
  - Organ interaction, adaptive narration, and quizzes.
- Roadmap Scene:
  - Accordion roadmap rendering, existing-vs-generated behavior, refresh support.

## 3. Backend Architecture

### Main modules

- `server.js`:
  - API routes, validation, orchestration, error handling.
- `agents.js`:
  - Agent prompt logic and JSON parsing.
- `db.js`:
  - MongoDB access layer, profile CRUD, roadmap persistence, feedback logs, and bounded learning preference updates.

### Data stores

- `users` collection:
  - Profile document including onboarding, organ history, quiz history, agent summary, learning preferences, and roadmap.
- `feedbackLogs` collection:
  - Raw feedback telemetry for auditing and analysis.

## 4. The 4 Agents

### Agent 1: Adaptive Learning Evaluator

- Backend function:
  - `computeAgentSummary(userProfile)`
- Trigger points:
  - After organ log updates and quiz submissions.
- Input signals:
  - Onboarding data, organ history, quiz history.
- Output:
  - `agentSummary` with overall level, weak concepts, strong concepts, and recommended next organ.
- Why it matters:
  - Personalization backbone for Agent 2, Agent 3, and Agent 4.

### Agent 2: AR Narrator

- Backend function:
  - `generateNarration(...)`
- Trigger points:
  - When user tracks an organ and requests narration pages.
- Input signals:
  - Organ name, learner level, weak concepts, session count, page index, recent narrations.
- Output:
  - Short adaptive narration text with anti-repetition behavior.
- Why it matters:
  - Personalized explanation in real-time AR interaction.

### Agent 3: Quiz Question Generator

- Backend function:
  - `generateQuestions(...)`
- Trigger points:
  - Quiz start from Unity.
- Input signals:
  - Organ, level, weak concepts, requested count, previous questions.
- Output:
  - MCQ list with 4 options, correct index, and explanation.
- Why it matters:
  - Converts learning progress into adaptive assessment.

### Agent 4: Learning Roadmap Generator

- Backend function:
  - `generateRoadmap(userProfile)`
- Trigger points:
  - Roadmap scene load and manual refresh.
- Input signals:
  - Onboarding, agent summary, and organ history.
- Output:
  - Structured roadmap: focus + step list + linked resources.
- Endpoint behavior:
  - Existing or generate: `GET /api/agent/roadmap-existing-or-generate/:userId`
  - Force regenerate: `GET /api/agent/roadmap/:userId`
- Why it matters:
  - Long-term guided plan built from the learner's real data.

## 5. Key API Surface (Unity-facing)

### Profile and progress

- `POST /api/profile/load`
- `POST /api/profile/save`
- `GET /api/profile/:userId`
- `POST /api/organ/log`
- `POST /api/quiz/submit`

### Agent endpoints

- `POST /api/agent/narrate` (Agent 2)
- `POST /api/agent/question` (Agent 3)
- `GET /api/agent/roadmap-existing-or-generate/:userId` (Agent 4 cached-first)
- `GET /api/agent/roadmap/:userId` (Agent 4 fresh generation)

### Feedback and personalization safety layer

- `POST /api/feedback`
- `GET /api/feedback/logs/:userId`
- `GET /api/profile/:userId/learning-preferences`
- `GET /api/agent/prompt-context/:userId`

## 6. Core Data Contract

The learner profile document combines:

- Identity: user id, name, email, photo
- Onboarding: grade, prior knowledge, learning goal
- Behavior telemetry: organHistory and quizHistory
- Agent state: agentSummary
- Guidance state: roadmap
- Preference controls: learningPreferences

Reference example schema is maintained in backend schema definition for integration alignment.

## 7. Primary Runtime Flows

### A. Learn + Narrate + Quiz loop

1. Unity logs into backend and loads profile.
2. User studies an organ in AR scene.
3. Agent 2 narration is generated from Agent 1-derived context.
4. User starts quiz; Agent 3 generates adaptive MCQs.
5. Quiz submission updates profile and recomputes Agent 1 summary.
6. Updated summary influences future narration, questions, and roadmap.

### B. Roadmap flow

1. Roadmap scene opens.
2. Unity calls existing-or-generate roadmap endpoint.
3. Backend returns either cached roadmap or generated roadmap.
4. User can press refresh to force a newly generated roadmap.

### C. Feedback loop

1. User submits feedback action.
2. Backend stores raw log entry.
3. If action is allowlisted, backend updates bounded learning preferences.
4. Prompt context endpoint exposes safe personalization variables for future responses.

## 8. Non-Functional Design Notes

- Personalization is persistent per user profile.
- Feedback updates are bounded and allowlisted to prevent unsafe prompt injection behavior.
- Agent outputs are validated/parsed to strict JSON where possible.
- Roadmap caching reduces unnecessary regeneration costs while preserving refresh control.
- Unity service layers handle request failures and show fallback UI states.

## 9. Source Map (Implementation Files)

### Unity

- `Assets/Scripts/Agent1/LearnerProfileManager.cs`
- `Assets/Scripts/Agent1/ProfileService.cs`
- `Assets/Scripts/Agent2/NarrationManager.cs`
- `Assets/Scripts/Agent2/NarrationService.cs`
- `Assets/Scripts/3D-OrganModelScene/Quiz/QuizUI.cs`
- `Assets/Scripts/Agent3/QuizService.cs`
- `Assets/Scripts/Agent1/RoadmapService.cs`
- `Assets/Scripts/RoadmapScene/RoadmapUI.cs`
- `Assets/Scripts/Feedback/FeedbackService.cs`

### Backend

- `Backend/server.js`
- `Backend/agents.js`
- `Backend/db.js`
- `Backend/schema.json`
