# AR-Anatomy Full System Architecture

This document describes the complete architecture of AR-Anatomy across Unity client, Node.js backend, AI agents, data layer, and runtime flows.

## 1. System Scope

AR-Anatomy is a multi-scene Unity application that personalizes learning through four AI agent workflows:

- Agent 1: learner profile evaluation and recommendation summary
- Agent 2: adaptive narration generation
- Agent 3: adaptive quiz generation
- Agent 4: personalized learning roadmap generation

The backend provides orchestration, persistent state, and LLM integration, while Unity handles real-time interaction, UI, and scene experiences.

## 2. End-to-End Architecture

```mermaid
flowchart LR
		U[Student Device: Android/Desktop] --> L[Login Scene]
		L --> S[Start Scene]
		S --> M[3D Anatomy Model Scene]
		S --> R[Roadmap Scene]

		subgraph UNITY[Unity Client]
			US[UserSession]
			PM[LearnerProfileManager\nAgent 1 Client Orchestrator]
			NM[NarrationManager + NarrationService\nAgent 2 Client]
			QZ[QuizUI + QuizService\nAgent 3 Client]
			RM[RoadmapUI + RoadmapService\nAgent 4 Client]
			FB[FeedbackService]
		end

		L --> US
		US --> PM
		M --> NM
		M --> QZ
		R --> RM
		S --> FB

		PM -->|POST /api/profile/load| API
		PM -->|POST /api/organ/log| API
		PM -->|POST /api/quiz/submit| API
		NM -->|POST /api/agent/narrate| API
		QZ -->|POST /api/agent/question| API
		RM -->|GET /api/agent/roadmap-existing-or-generate/:userId| API
		RM -->|GET /api/agent/roadmap/:userId| API
		FB -->|POST /api/feedback| API

		subgraph BACKEND[Azure-Hosted Backend (Node.js/Express)]
			API[server.js API Layer]
			AG[agents.js\nAgent prompt + parsing layer]
			DBL[db.js\nData access and persistence]
			A1[Agent 1\ncomputeAgentSummary]
			A2[Agent 2\ngenerateNarration]
			A3[Agent 3\ngenerateQuestions]
			A4[Agent 4\ngenerateRoadmap]
			MDB[(MongoDB\nusers + feedbackLogs)]
			LLM[(Azure OpenAI)]
		end

		API --> A1
		API --> A2
		API --> A3
		API --> A4
		API --> DBL
		AG --> LLM
		A1 --> AG
		A2 --> AG
		A3 --> AG
		A4 --> AG
		DBL --> MDB
```

## 3. Unity Client Architecture

### 3.1 Scene Responsibility Model

- Login Scene
	- Authentication bootstrap (Google or guest)
	- Session initialization
	- Profile loading before routing
- Start Scene
	- Main navigation hub
	- Dashboard and feedback entry points
- 3D Anatomy Model Scene
	- Organ interaction session
	- Narration requests and rendering
	- Quiz request and submission
- Roadmap Scene
	- Cached-first roadmap retrieval
	- Manual roadmap regeneration
	- Accordion visualization of roadmap steps/resources

### 3.2 Core Runtime Components

- UserSession
	- Holds identity, auth mode, and current user context
- LearnerProfileManager
	- Central state manager for profile and summary
	- Handles profile load, organ log updates, quiz submit updates
	- Emits events used by dashboard and other UI systems
- NarrationManager / NarrationService
	- Builds contextual payload from profile summary + organ context
	- Fetches adaptive narration pages
- QuizUI / QuizService
	- Requests adaptive MCQs
	- Submits quiz outcomes to update profile and Agent 1 summary
- RoadmapUI / RoadmapService
	- Requests existing roadmap or generates new roadmap
	- Supports force-refresh behavior for new planning
- FeedbackService
	- Sends raw feedback plus bounded action tags

## 4. Backend Architecture

The backend is deployed on Microsoft Azure and exposes API endpoints to the Unity client.

### 4.1 API Layer (Express)

The API layer is the orchestration boundary between Unity and AI/data subsystems.

Primary route groups:

- Profile and progress
	- POST /api/profile/load
	- POST /api/profile/save
	- GET /api/profile/:userId
	- POST /api/organ/log
	- POST /api/quiz/submit
- Agent endpoints
	- POST /api/agent/narrate
	- POST /api/agent/question
	- GET /api/agent/roadmap-existing-or-generate/:userId
	- GET /api/agent/roadmap/:userId
- Feedback and prompt context
	- POST /api/feedback
	- GET /api/feedback/logs/:userId
	- GET /api/profile/:userId/learning-preferences
	- GET /api/agent/prompt-context/:userId

### 4.2 Agent Layer

- Agent 1: computeAgentSummary(userProfile)
	- Produces overall level, strong/weak concepts, recommended next organ
- Agent 2: generateNarration(...)
	- Produces short adaptive teaching text for the current organ context
- Agent 3: generateQuestions(...)
	- Produces adaptive MCQ set with options, answer index, explanation
- Agent 4: generateRoadmap(userProfile)
	- Produces structured roadmap with focus, steps, and resources

### 4.3 Data Access Layer

db.js handles persistence and query operations for:

- users collection
	- Identity and onboarding
	- Organ history and quiz history
	- Agent summary
	- Learning preferences
	- Roadmap state
- feedbackLogs collection
	- Raw feedback telemetry for analysis/audit

## 5. Data Contracts

### 5.1 Learner Profile (users)

Main fields:

- user identity: id, displayName, email, photoUrl
- onboarding: grade, prior knowledge, learning goals
- organHistory: studied organs, time/session metadata
- quizHistory: score and concept-level performance
- agentSummary: level, weak/strong concepts, recommendation
- roadmap: saved roadmap state
- learningPreferences: bounded safe preference controls

### 5.2 Feedback Safety Model

Dual path design:

- Path A: raw text goes to feedbackLogs for developer telemetry
- Path B: only allowlisted action tags can update bounded user preferences

This prevents direct prompt injection into runtime LLM context.

## 6. Core Runtime Flows

### 6.1 Login and Bootstrap

1. User signs in (Google or guest)
2. UserSession is populated
3. LearnerProfileManager loads profile for user
4. App routes to onboarding or home depending on profile state

### 6.2 Learn -> Narrate -> Quiz -> Adapt

1. User enters 3D scene and studies an organ
2. Agent 2 generates adaptive narration from user context
3. Agent 3 generates adaptive quiz questions
4. Quiz submission updates profile and recomputes Agent 1 summary
5. Updated summary affects future narration, quiz difficulty, and roadmap

### 6.3 Roadmap Retrieval and Refresh

1. Roadmap scene opens
2. Client requests existing-or-generate roadmap endpoint
3. Backend returns cached roadmap if present, otherwise generates and stores
4. User can force refresh to regenerate roadmap

### 6.4 Feedback Loop

1. Unity submits feedback payload
2. Backend always logs telemetry entry
3. Backend applies only allowlisted bounded preference updates
4. Prompt-context route exposes safe variables for future personalization

## 7. Non-Functional Characteristics

- Personalization persistence per user profile
- Cached roadmap strategy to reduce unnecessary LLM usage
- Bounded preference updates to protect model behavior
- JSON-structured agent responses where possible for stable parsing
- Unity-side fallback UI handling for backend/network failures

## 8. Source Map

### Unity

- Assets/Scripts/Login/LoginManager.cs
- Assets/Scripts/Login/ProfileDashboardUI.cs
- Assets/Scripts/Agent1/LearnerProfileManager.cs
- Assets/Scripts/Agent1/ProfileService.cs
- Assets/Scripts/Agent2/NarrationManager.cs
- Assets/Scripts/Agent2/NarrationService.cs
- Assets/Scripts/3D-OrganModelScene/Quiz/QuizUI.cs
- Assets/Scripts/Agent3/QuizService.cs
- Assets/Scripts/Agent1/RoadmapService.cs
- Assets/Scripts/RoadmapScene/RoadmapUI.cs
- Assets/Scripts/Feedback/FeedbackService.cs

### Backend

- Backend/server.js
- Backend/agents.js
- Backend/db.js
- Backend/schema.json
- Backend/state.js

## 9. Deployment View

- Client runtime: Unity app (Android/Desktop)
- API runtime: Azure-hosted Node.js Express backend (Function App/App Service)
- Data runtime: MongoDB
- AI runtime: Azure OpenAI

This architecture keeps real-time interaction in Unity while centralizing intelligence, persistence, and orchestration in the backend.
