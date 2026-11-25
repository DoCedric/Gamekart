using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug free-fly camera. Can live on any GameObject: it takes over the scene's
/// KartCameraFollow camera (or Camera.main if there is none).
/// Trigger the toggle action to detach the camera from the kart and fly around;
/// trigger it again to snap back to the follow camera.
///
/// All controls come from Input System actions assigned in the inspector.
/// Expected action types:
///   toggleAction      Button
///   moveAction        Value / Vector2   (x = strafe, y = forward)
///   verticalAction    Value / Axis      (+1 = up, -1 = down)
///   lookAction        Value / Vector2   (e.g. Mouse delta)
///   fastAction        Button            (held)
///   speedAction       Value / Axis      (e.g. Mouse scroll Y; only the sign is used)
///   freezeTimeAction  Button
/// Only toggleAction is required; any other can be left empty.
///
/// Player kart input is disabled while flying so the move keys don't also drive the kart.
/// </summary>
public class FreeFlyCamera : MonoBehaviour
{
    [Header("Actions")]
    [SerializeField] InputActionReference toggleAction;
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference verticalAction;
    [SerializeField] InputActionReference lookAction;
    [SerializeField] InputActionReference fastAction;
    [SerializeField] InputActionReference speedAction;
    [SerializeField] InputActionReference freezeTimeAction;

    [Header("Movement")]
    [Tooltip("Base fly speed in m/s.")]
    [SerializeField] float moveSpeed = 15f;
    [Tooltip("Speed multiplier while the fast action is held.")]
    [SerializeField] float fastMultiplier = 4f;
    [Tooltip("How much one speed-action step (scroll notch) scales the base speed.")]
    [SerializeField] float speedStep = 1.2f;
    [SerializeField] float minSpeed = 1f;
    [SerializeField] float maxSpeed = 200f;

    [Header("Look")]
    [Tooltip("Degrees per unit of look input (per pixel for mouse delta).")]
    [SerializeField] float lookSensitivity = 0.15f;

    [Header("Camera")]
    [Tooltip("The follow camera to take over. Leave empty to use the KartCameraFollow on this object, or the first one in the scene.")]
    [SerializeField] KartCameraFollow follow;

    public bool IsFlying { get; private set; }

    // The transform actually being flown: the follow camera's, or Camera.main as a fallback.
    Transform cam;
    KartInputReader[] disabledReaders = System.Array.Empty<KartInputReader>();
    float yaw;
    float pitch;
    bool timeFrozen;
    float timeScaleBeforeFreeze = 1f;

    void Awake()
    {
        if (follow == null) follow = GetComponent<KartCameraFollow>();
        if (follow == null) follow = FindAnyObjectByType<KartCameraFollow>();

        if (follow != null) cam = follow.transform;
        else if (Camera.main != null) cam = Camera.main.transform;
        else Debug.LogWarning("FreeFlyCamera: no KartCameraFollow or main camera found.", this);
    }

    void OnEnable()
    {
        // Only the toggle needs to listen while not flying; the rest is enabled in SetFlying.
        Enable(toggleAction);
    }

    void OnDisable()
    {
        if (IsFlying) SetFlying(false);
        Disable(toggleAction);
    }

    void Update()
    {
        if (cam == null) return;

        if (WasPressed(toggleAction))
            SetFlying(!IsFlying);

        if (!IsFlying) return;

        if (WasPressed(freezeTimeAction))
            SetTimeFrozen(!timeFrozen);

        Look();
        AdjustSpeed();
        Move();
    }

    void Look()
    {
        Vector2 delta = Read<Vector2>(lookAction) * lookSensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, -89f, 89f);
        cam.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void AdjustSpeed()
    {
        float step = Read<float>(speedAction);
        if (step > 0f) moveSpeed = Mathf.Min(moveSpeed * speedStep, maxSpeed);
        else if (step < 0f) moveSpeed = Mathf.Max(moveSpeed / speedStep, minSpeed);
    }

    void Move()
    {
        Vector2 planar = Read<Vector2>(moveAction);
        float vertical = Read<float>(verticalAction);

        Vector3 input = new Vector3(planar.x, vertical, planar.y);
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool fast = fastAction != null && fastAction.action.IsPressed();
        float speed = moveSpeed * (fast ? fastMultiplier : 1f);

        // Forward/strafe follow the camera's view; up/down stays world-vertical.
        Vector3 move = cam.forward * input.z + cam.right * input.x + Vector3.up * input.y;

        // Unscaled so flying still works while time is frozen.
        cam.position += move * speed * Time.unscaledDeltaTime;
    }

    void SetFlying(bool flying)
    {
        IsFlying = flying;

        if (follow != null) follow.enabled = !flying;

        if (flying)
        {
            Vector3 euler = cam.rotation.eulerAngles;
            yaw = euler.y;
            pitch = euler.x > 180f ? euler.x - 360f : euler.x;

            // Disable kart input first: it disables its own actions, which may share
            // bindings/actions with ours, so ours must be enabled afterwards.
            disabledReaders = FindObjectsByType<KartInputReader>(FindObjectsSortMode.None);
            foreach (KartInputReader reader in disabledReaders)
                reader.enabled = false;

            Enable(moveAction);
            Enable(verticalAction);
            Enable(lookAction);
            Enable(fastAction);
            Enable(speedAction);
            Enable(freezeTimeAction);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Disable(moveAction);
            Disable(verticalAction);
            Disable(lookAction);
            Disable(fastAction);
            Disable(speedAction);
            Disable(freezeTimeAction);

            foreach (KartInputReader reader in disabledReaders)
                if (reader != null) reader.enabled = true;
            disabledReaders = System.Array.Empty<KartInputReader>();

            if (timeFrozen) SetTimeFrozen(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void SetTimeFrozen(bool frozen)
    {
        timeFrozen = frozen;
        if (frozen)
        {
            timeScaleBeforeFreeze = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleBeforeFreeze;
        }
    }

    static void Enable(InputActionReference reference)
    {
        if (reference != null) reference.action.Enable();
    }

    static void Disable(InputActionReference reference)
    {
        if (reference != null) reference.action.Disable();
    }

    static bool WasPressed(InputActionReference reference)
    {
        return reference != null && reference.action.WasPressedThisFrame();
    }

    static T Read<T>(InputActionReference reference) where T : struct
    {
        return reference != null ? reference.action.ReadValue<T>() : default;
    }
}
