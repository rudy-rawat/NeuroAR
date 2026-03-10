using UnityEngine;

public class PinchToZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 0.01f;
    public float minScaleMultiplier = 0.5f;  // Minimum multiplier (0.5x original)
    public float maxScaleMultiplier = 3f;    // Maximum multiplier (3x original)

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    public bool enableRotation = true;

    private bool isTwoFingerGesture = false;
    private float previousTouchDistance;
    private Vector3 initialScale;      // Store initial scale at start
    private Vector3 currentScale;      // Store current scale
    private float currentScaleMultiplier = 1f; // Track multiplier instead of absolute scale

    void Start()
    {
        initialScale = transform.localScale;
        currentScale = initialScale;
    }

    void Update()
    {
        HandleTouchInput();

#if UNITY_EDITOR
        HandleMouseInput();
#endif
    }

    void HandleTouchInput()
    {
        int touchCount = Input.touchCount;
        if (touchCount == 0)
        {
            if (isTwoFingerGesture)
            {
                // Touch ended, maintain current scale
                isTwoFingerGesture = false;
                currentScale = transform.localScale;
            }
            return;
        }

        if (touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (touchCount == 2)
        {
            HandleTwoFingerGestures();
        }
    }

    void HandleSingleTouch()
    {
        if (isTwoFingerGesture) return;

        Touch touch = Input.GetTouch(0);
        if (enableRotation && touch.phase == TouchPhase.Moved)
        {
            // Rotate model only around Y-axis (vertical/middle axis)
            Vector2 touchDelta = touch.deltaPosition;
            float rotationY = -touchDelta.x * rotationSpeed * Time.deltaTime;
            // Only rotate around Y-axis (up vector)
            transform.Rotate(Vector3.up, rotationY, Space.World);
        }
    }

    void HandleTwoFingerGestures()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            isTwoFingerGesture = true;
            previousTouchDistance = Vector2.Distance(touch0.position, touch1.position);
            return;
        }

        if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);
            float deltaDistance = currentDistance - previousTouchDistance;

            if (Mathf.Abs(deltaDistance) > 1f) // Threshold to avoid jitter
            {
                // Calculate new multiplier
                float scaleFactor = 1 + deltaDistance * zoomSpeed * 0.01f;
                float newMultiplier = currentScaleMultiplier * scaleFactor;

                // Clamp multiplier between min and max
                newMultiplier = Mathf.Clamp(newMultiplier, minScaleMultiplier, maxScaleMultiplier);

                // Apply multiplier to initial scale
                Vector3 newScale = initialScale * newMultiplier;
                transform.localScale = newScale;

                currentScaleMultiplier = newMultiplier;
                currentScale = newScale;
            }

            previousTouchDistance = currentDistance;
        }
    }

#if UNITY_EDITOR
    void HandleMouseInput()
    {
        // Mouse scroll for zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float scaleFactor = 1 + scroll * zoomSpeed;
            float newMultiplier = currentScaleMultiplier * scaleFactor;

            // Clamp multiplier between min and max
            newMultiplier = Mathf.Clamp(newMultiplier, minScaleMultiplier, maxScaleMultiplier);

            // Apply multiplier to initial scale
            Vector3 newScale = initialScale * newMultiplier;
            transform.localScale = newScale;

            currentScaleMultiplier = newMultiplier;
            currentScale = newScale;
        }

        // Mouse drag for rotation - only Y-axis
        if (Input.GetMouseButton(0) && enableRotation)
        {
            float mouseX = Input.GetAxis("Mouse X");
            // Only rotate around Y-axis (up vector)
            transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
        }
    }
#endif
}