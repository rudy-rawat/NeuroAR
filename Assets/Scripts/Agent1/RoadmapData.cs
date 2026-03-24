using System;
using System.Collections.Generic;

[Serializable]
public class RoadmapResource
{
    public string name;
    public string type; // "organ_scene" or "external_link"
    public string referenceId; // organ_name (if organ_scene)
    public string url; // url (if external_link)
}

[Serializable]
public class RoadmapStep
{
    public int stepNumber;
    public string topic;
    public string description;
    public int estimatedTimeInMinutes;
    public List<RoadmapResource> resources;
}

[Serializable]
public class RoadmapResponse
{
    public string roadmapId;
    public string generatedAt;
    public string overallFocus;
    public string source; // populated in Unity when using existing-or-generate endpoint
    public string learningNarrative; // optional backend field
    public string studyAdvice; // optional backend field
    public List<RoadmapStep> steps;
}

[Serializable]
public class RoadmapEnvelopeResponse
{
    public string source;
    public RoadmapResponse roadmap;
}
