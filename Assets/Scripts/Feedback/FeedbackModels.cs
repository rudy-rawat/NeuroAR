using System;

[Serializable]
public class FeedbackRequest
{
    public string userId;
    public string agentId;
    public string rawFeedbackText;
    public string suggestedAction;
    public FeedbackMetadata metadata;
}

[Serializable]
public class FeedbackMetadata
{
    public string sessionId;
    public string clientVersion;
    public string sceneName;
    public string platform;
}

[Serializable]
public class FeedbackResponse
{
    public string message;
    public string appliedAction;
    public LearningPreferences learningPreferences;
}

[Serializable]
public class PromptContextResponse
{
    public string userId;
    public string topic;
    public LearningPreferences learningPreferences;
    public string systemPrompt;
}
