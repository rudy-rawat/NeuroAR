using UnityEngine;

[System.Serializable]
public class OrganVariant
{
    public string organName;              // e.g., "heart"
    public string organDescription;       // description of the organ
    public GameObject basicPrefab;        // basic outer heart
    public GameObject detailedPrefab;     // dissected inner heart

    [Header("Organ Information")]
    [TextArea(5, 10)]
    public string organInfo;              // General information about the organ displayed in Info Panel
}