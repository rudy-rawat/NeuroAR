using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OrganLabelManager : MonoBehaviour
{
    [Header("Label Prefab")]
    public GameObject labelPrefab;  // Assign the UI label prefab in inspector

    [Header("Label Settings")]
    public Canvas labelCanvas;      // Canvas to hold all labels (should be World Space)
    public Material lineMaterial;   // Material for the connecting lines
    public float labelDistance = 0.05f; // Distance from anchor point to label (in world units)
    public bool showLabels = true;  // Toggle to show/hide all labels
    public bool faceCameraX = true; // Make labels face camera on X-axis distribution

    private List<LabelUI> activeLabelUIs = new List<LabelUI>();
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void SetupLabels(LabelPoint[] labelPoints)
    {
        ClearLabels();

        if (!showLabels || labelPoints == null || labelPoints.Length == 0)
        {
            Debug.Log("No labels to setup or labels disabled");
            return;
        }

        Debug.Log($"Setting up {labelPoints.Length} labels");

        // Distribute labels: even indices on -X side, odd indices on +X side
        for (int i = 0; i < labelPoints.Length; i++)
        {
            var point = labelPoints[i];

            if (point.anchorPoint == null)
            {
                Debug.LogWarning($"Label '{point.labelText}' anchor point is null, skipping...");
                continue;
            }

            // Determine which side this label should be on
            bool isRightSide = (i % 2 == 1); // Odd indices go to right (+X), even to left (-X)

            Debug.Log($"Creating label '{point.labelText}' at anchor position: {point.anchorPoint.position} ({(isRightSide ? "+X" : "-X")} side)");
            CreateLabel(point, isRightSide);
        }

        Debug.Log($"Total active labels: {activeLabelUIs.Count}");
    }

    private void CreateLabel(LabelPoint labelPoint, bool isRightSide)
    {
        if (labelPrefab == null || labelCanvas == null)
        {
            Debug.LogError("Label prefab or canvas not assigned!");
            return;
        }

        GameObject labelObj = Instantiate(labelPrefab, labelCanvas.transform);
        LabelUI labelUI = labelObj.GetComponent<LabelUI>();

        if (labelUI == null)
        {
            labelUI = labelObj.AddComponent<LabelUI>();
        }

        // Pass isRightSide flag to LabelUI
        labelUI.Initialize(labelPoint, labelDistance, lineMaterial, isRightSide, faceCameraX);
        activeLabelUIs.Add(labelUI);
    }

    public void ClearLabels()
    {
        foreach (var label in activeLabelUIs)
        {
            if (label != null)
                Destroy(label.gameObject);
        }
        activeLabelUIs.Clear();
    }

    public void ToggleLabels(bool visible)
    {
        showLabels = visible;
        foreach (var label in activeLabelUIs)
        {
            if (label != null)
                label.gameObject.SetActive(visible);
        }
    }

    void LateUpdate()
    {
        // Update all label positions every frame
        foreach (var label in activeLabelUIs)
        {
            if (label != null)
                label.UpdatePosition(mainCamera, faceCameraX);
        }
    }

    void OnDestroy()
    {
        ClearLabels();
    }
}