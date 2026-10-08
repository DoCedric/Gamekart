using UnityEngine;

public class KartSelectorSpin : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float idleRotationSpeed = 30f;   // degrees per second for idle rotation
    public float spinDuration = 1f;         // time it takes to complete the quick spin
    public AnimationCurve spinCurve;        // curve for smooth in/out

    [Header("Drag Settings")]
    public float dragSensitivity = 0.3f;
    public float maxTiltAngle = 30f;
    public float tiltResetSpeed = 5f;

    private bool isSpinning = false;
    private float spinTimer = 0f;
    private float lastCurveValue = 0f;

    private bool isDragging = false;
    private Vector2 lastMousePos;

    private Quaternion originalRotation;


    private void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        HandleInput();

        if (isDragging)
        {
            HandleDragRotation();
        }
        else
        {
            HandleIdleRotation();
        }

        if (isSpinning)
        {
            HandleSpin();
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !isSpinning && !isDragging)
            {
                isSpinning = true;
                spinTimer = 0f;
                lastCurveValue = 0f;
            }
        }

        void HandleIdleRotation()
        {
            transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);
        }

        void HandleDragRotation()
        {
            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            // Horizontal drag → Y-axis rotation
            float yRotation = -mouseDelta.x * dragSensitivity;
            transform.Rotate(Vector3.up, yRotation, Space.World);

            Vector3 euler = transform.eulerAngles;
            transform.eulerAngles = euler;
        }

        void HandleSpin()
        {
            spinTimer += Time.deltaTime;
            float t = spinTimer / spinDuration;

            if (t >= 1f)
            {
                isSpinning = false;
            }
            else
            {
                float curveValue = spinCurve.Evaluate(t);
                float deltaCurve = curveValue - lastCurveValue;
                lastCurveValue = curveValue;

                float extraRotation = 360f * deltaCurve;
                transform.Rotate(Vector3.up, extraRotation, Space.World);
            }
        }
    }
}

