using UnityEngine;
using UnityEngine.InputSystem;

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
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    isDragging = true;
                    lastMousePos = mouse.position.ReadValue();
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    isDragging = false;
                }
            }

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && !isSpinning && !isDragging)
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
            if (Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = mousePos - lastMousePos;
            lastMousePos = mousePos;

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

