using UnityEngine;

[System.Serializable]
public class LabelPoint
{
    public string labelText;           // Text to display (e.g., "Left Ventricle")
    [HideInInspector]
    public Transform anchorPoint;      // Will be found automatically in the spawned model
    public Color labelColor = Color.white;  // Optional: customize label color

    // This will be used to match with GameObject names in the model
    // If empty, it will use labelText to find the anchor
    public string anchorName;

    public string GetAnchorSearchName()
    {
        return string.IsNullOrEmpty(anchorName) ? labelText : anchorName;
    }
}