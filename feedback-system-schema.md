## 1) `feedbackLogs` Collection (Raw Telemetry)

Collection name:

- `MONGODB_COLLECTION_FEEDBACK_LOGS` (if configured), otherwise
- `feedbackLogs`

### Document shape

```json
{
  "_id": "ObjectId",
  "userId": "user_12345",
  "agentId": "anatomy_tutor_agent",
  "rawFeedbackText": "The explanation was too advanced.",
  "suggestedAction": "decrease_complexity",
  "metadata": {
    "sessionId": "sess_001",
    "clientVersion": "1.2.0"
  },
  "timestamp": "2026-03-24T12:00:00.000Z",
  "createdAt": "2026-03-24T12:00:00.000Z"
}
```

### Fields

| Field | Type | Purpose |
|---|---|---|
| `_id` | ObjectId | Primary key |
| `userId` | String | User identifier |
| `agentId` | String | Agent/source that received feedback |
| `rawFeedbackText` | String | Raw user text |
| `suggestedAction` | String | Structured action tag |
| `metadata` | Object | Optional context data |
| `timestamp` | ISO String | Serialized event time |
| `createdAt` | Date | Native date used for sorting |

## 2) `users.learningPreferences` (Bounded User State)

Stored inside each user profile document as:

```json
"learningPreferences": {
  "complexityLevel": 5,
  "pacing": "moderate",
  "visualDependency": false
}
```

### Field constraints

| Field | Type | Constraint | Default |
|---|---|---|---|
| `complexityLevel` | Integer | Clamped to `1..10` | `5` |
| `pacing` | String | `slow` \| `moderate` \| `fast` | `moderate` |
| `visualDependency` | Boolean | `true` or `false` | `false` |
