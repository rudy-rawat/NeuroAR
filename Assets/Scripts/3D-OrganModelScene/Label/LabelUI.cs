using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LabelUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public TextMeshProUGUI labelText;

    [Header("Line Settings")]
    public Material lineMaterial;
    public float lineWidth = 0.002f;

    private LabelPoint labelPoint;
    private Camera mainCamera;
    private Canvas canvas;
    private float labelDistance;
    private LineRenderer lineRenderer;
    private bool isRightSide;
    private bool faceCameraX;

    public void Initialize(LabelPoint point, float distance, Material lineMat, bool isRightSide, bool faceCameraX)
    {
        labelPoint = point;
        labelDistance = distance;
        lineMaterial = lineMat;
        mainCamera = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        this.isRightSide = isRightSide;
        this.faceCameraX = faceCameraX;

        // Get text from anchor GameObject name
        if (labelPoint.anchorPoint != null)
        {
            string anchorName = labelPoint.anchorPoint.name;
            if (labelText != null)
            {
                labelText.text = anchorName;
                labelText.color = labelPoint.labelColor;
            }
        }

        // Setup background
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }

        // Add FaceCamera to this root so it faces camera (if enabled)
        if (faceCameraX)
        {
            if (GetComponent<FaceCamera>() == null)
            {
                gameObject.AddComponent<FaceCamera>();
            }
        }
        else
        {
            // Remove FaceCamera if not needed
            FaceCamera faceCamera = GetComponent<FaceCamera>();
            if (faceCamera != null)
            {
                Destroy(faceCamera);
            }
        }

        // Create line renderer
        CreateLine();
        Debug.Log($"Label initialized: {labelText.text} on {(isRightSide ? "+X" : "-X")} side");
    }

    private void CreateLine()
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = labelPoint.labelColor;
        lineRenderer.endColor = labelPoint.labelColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    public void UpdatePosition(Camera cam, bool faceCameraX)
    {
        if (labelPoint.anchorPoint == null || cam == null)
            return;

        mainCamera = cam;

        Vector3 anchorWorldPos = labelPoint.anchorPoint.position;

        // Check if behind camera
        Vector3 viewPos = mainCamera.WorldToViewportPoint(anchorWorldPos);
        if (viewPos.z < 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Calculate label position based on which side it should be on
        Vector3 labelWorldPos;

        if (isRightSide)
        {
            // +X side (right side)
            labelWorldPos = anchorWorldPos + Vector3.right * labelDistance;
        }
        else
        {
            // -X side (left side)
            labelWorldPos = anchorWorldPos + Vector3.left * labelDistance;
        }

        // Position label
        transform.position = labelWorldPos;

        // Update line positions
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, anchorWorldPos);
            lineRenderer.SetPosition(1, labelWorldPos);
        }
    }
}