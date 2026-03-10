//using UnityEngine;

//public class ARTouchControls : MonoBehaviour
//{
//    [Header("Zoom Settings")]
//    public float zoomSensitivity = 1.0f;
//    public float minScale = 0.3f;
//    public float maxScale = 5.0f;

//    [Header("Rotation Settings")]
//    public float rotationSpeed = 100f;
//    public bool enableRotation = true;

//    [Header("Pan Settings")]
//    public float panSensitivity = 1.0f;
//    public bool enablePanning = true;

//    private Vector3 initialScale;
//    private Vector3 initialPosition;
//    private Quaternion initialRotation;

//    // Touch state tracking
//    private bool isTwoFingerGesture = false;
//    private Vector2 previousTouch1Position;
//    private Vector2 previousTouch2Position;
//    private float previousTouchDistance;

//    void Start()
//    {
//        initialScale = transform.localScale;
//        initialPosition = transform.localPosition;
//        initialRotation = transform.localRotation;
//    }

//    void Update()
//    {
//        HandleTouchInput();

//#if UNITY_EDITOR
//        HandleMouseInput();
//#endif
//    }

//    void HandleTouchInput()
//    {
//        int touchCount = Input.touchCount;

//        if (touchCount == 0)
//        {
//            isTwoFingerGesture = false;
//            return;
//        }

//        if (touchCount == 1)
//        {
//            HandleSingleTouch();
//        }
//        else if (touchCount == 2)
//        {
//            HandleTwoFingerGestures();
//        }
//    }

//    void HandleSingleTouch()
//    {
//        if (isTwoFingerGesture) return;

//        Touch touch = Input.GetTouch(0);

//        if (enableRotation)
//        {
//            if (touch.phase == TouchPhase.Moved)
//            {
//                // Rotate model based on touch delta
//                Vector2 touchDelta = touch.deltaPosition;

//                float rotationY = -touchDelta.x * rotationSpeed * Time.deltaTime;
//                float rotationX = touchDelta.y * rotationSpeed * Time.deltaTime;

//                transform.Rotate(Camera.main.transform.up, rotationY, Space.World);
//                transform.Rotate(Camera.main.transform.right, rotationX, Space.World);
//            }
//        }
//    }

//    void HandleTwoFingerGestures()
//    {
//        Touch touch1 = Input.GetTouch(0);
//        Touch touch2 = Input.GetTouch(1);

//        Vector2 touch1Position = touch1.position;
//        Vector2 touch2Position = touch2.position;

//        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
//        {
//            isTwoFingerGesture = true;
//            previousTouch1Position = touch1Position;
//            previousTouch2Position = touch2Position;
//            previousTouchDistance = Vector2.Distance(touch1Position, touch2Position);
//            return;
//        }

//        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
//        {
//            // Handle Pinch-to-Zoom
//            float currentDistance = Vector2.Distance(touch1Position, touch2Position);
//            float deltaDistance = currentDistance - previousTouchDistance;

//            if (Mathf.Abs(deltaDistance) > 1f) // Threshold to avoid jitter
//            {
//                float scaleFactor = 1 + (deltaDistance * zoomSensitivity * 0.01f);
//                Vector3 newScale = transform.localScale * scaleFactor;

//                // Clamp scale
//                float scaleX = Mathf.Clamp(newScale.x, initialScale.x * minScale, initialScale.x * maxScale);
//                float scaleY = Mathf.Clamp(newScale.y, initialScale.y * minScale, initialScale.y * maxScale);
//                float scaleZ = Mathf.Clamp(newScale.z, initialScale.z * minScale, initialScale.z * maxScale);

//                transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
//            }

//            // Handle Two-Finger Pan
//            if (enablePanning)
//            {
//                Vector2 touch1Delta = touch1Position - previousTouch1Position;
//                Vector2 touch2Delta = touch2Position - previousTouch2Position;
//                Vector2 averageDelta = (touch1Delta + touch2Delta) * 0.5f;

//                if (averageDelta.magnitude > 5f) // Threshold to avoid jitter
//                {
//                    Vector3 worldDelta = Camera.main.ScreenToWorldPoint(new Vector3(averageDelta.x, averageDelta.y, Camera.main.nearClipPlane + 1f));
//                    worldDelta -= Camera.main.ScreenToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane + 1f));

//                    transform.position += worldDelta * panSensitivity;
//                }
//            }

//            previousTouchDistance = currentDistance;
//        }

//        previousTouch1Position = touch1Position;
//        previousTouch2Position = touch2Position;
//    }

//#if UNITY_EDITOR
//    void HandleMouseInput()
//    {
//        // Mouse scroll for zoom
//        float scroll = Input.GetAxis("Mouse ScrollWheel");
//        if (scroll != 0)
//        {
//            float scaleFactor = 1 + (scroll * zoomSensitivity);
//            Vector3 newScale = transform.localScale * scaleFactor;

//            float scaleX = Mathf.Clamp(newScale.x, initialScale.x * minScale, initialScale.x * maxScale);
//            float scaleY = Mathf.Clamp(newScale.y, initialScale.y * minScale, initialScale.y * maxScale);
//            float scaleZ = Mathf.Clamp(newScale.z, initialScale.z * minScale, initialScale.z * maxScale);

//            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
//        }

//        // Mouse drag for rotation
//        if (Input.GetMouseButton(0) && enableRotation)
//        {
//            float mouseX = Input.GetAxis("Mouse X");
//            float mouseY = Input.GetAxis("Mouse Y");

//            transform.Rotate(Camera.main.transform.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
//            transform.Rotate(Camera.main.transform.right, mouseY * rotationSpeed * Time.deltaTime, Space.World);
//        }

//        // Right mouse drag for panning
//        if (Input.GetMouseButton(1) && enablePanning)
//        {
//            float mouseX = Input.GetAxis("Mouse X");
//            float mouseY = Input.GetAxis("Mouse Y");

//            // Direct position modification
//            Vector3 screenMovement = new Vector3(-mouseX, -mouseY, 0) * panSensitivity * Time.deltaTime;
//            transform.position += Camera.main.transform.right * screenMovement.x;
//            transform.position += Camera.main.transform.up * screenMovement.y;
//        }
//    }
//#endif

//    public void ResetModel()
//    {
//        transform.localScale = initialScale;
//        transform.localPosition = initialPosition;
//        transform.localRotation = initialRotation;
//    }
//}
