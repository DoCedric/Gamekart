using UnityEngine;
using UnityEngine.InputSystem;
using KartAcademy.Core;

public class KartController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private KartConfig kartConfig;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference driftAction;

    [Header("Physics")]
    [SerializeField] private float angularDampingValue = 2f;
    [SerializeField] private float velocityDampenFactor = 0.9f;
    [SerializeField] private float stabilityStrength = 5f;
    [SerializeField] private float groundAdhesionSpeed = 5f;

    private Rigidbody rb;
    private GroundDetector groundDetector;
    private MovementSystem movementSystem;
    private SteeringSystem steeringSystem;
    private AirborneSystem airborneSystem;
    private DriftSystem driftSystem;
    private KartState currentState = KartState.Grounded;
    private int coinCount = 0;

    public KartState CurrentState => currentState;
    public float CurrentSpeed => movementSystem.CurrentSpeed;
    public float TargetSpeed => movementSystem.TargetSpeed;
    public GroundDetector GroundDetector => groundDetector;
    public int CoinCount => coinCount;
    public DriftSystem DriftSystem => driftSystem;

    private void Start()
    {
        if (kartConfig == null)
        {
            Debug.LogError("KartController: No KartConfig assigned!", this);
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        groundDetector = GetComponent<GroundDetector>();
        movementSystem = GetComponent<MovementSystem>();
        steeringSystem = GetComponent<SteeringSystem>();
        airborneSystem = GetComponent<AirborneSystem>();
        driftSystem = GetComponent<DriftSystem>();

        if (rb == null)
        {
            Debug.LogError("KartController: Rigidbody not found!", this);
            enabled = false;
            return;
        }

        if (groundDetector == null)
        {
            Debug.LogError("KartController: GroundDetector not found!", this);
            enabled = false;
            return;
        }

        // Add systems if they don't exist
        if (movementSystem == null)
        {
            movementSystem = gameObject.AddComponent<MovementSystem>();
        }
        if (steeringSystem == null)
        {
            steeringSystem = gameObject.AddComponent<SteeringSystem>();
        }
        if (airborneSystem == null)
        {
            airborneSystem = gameObject.AddComponent<AirborneSystem>();
        }
        if (driftSystem == null)
        {
            driftSystem = gameObject.AddComponent<DriftSystem>();
        }

        rb.angularDamping = angularDampingValue;

        // Initialize systems
        movementSystem.Initialize(kartConfig, groundDetector);
        steeringSystem.Initialize(kartConfig, movementSystem);
        airborneSystem.Initialize(kartConfig, steeringSystem);
        driftSystem.Initialize(kartConfig, movementSystem, steeringSystem, groundDetector);
    }

    private void FixedUpdate()
    {
        if (kartConfig == null) return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        bool driftHeld = driftAction != null && driftAction.action.IsPressed();
        bool driftPressed = driftAction != null && driftAction.action.WasPressedThisFrame();

        // Drive the drift/mini-turbo state machine - this is the single source of truth
        // for drift state, KartController just reflects it into KartState below.
        driftSystem.Tick(input.x, driftPressed, driftHeld, groundDetector.IsGrounded, rb, Time.fixedDeltaTime);

        UpdateState();

        HandleCurrentState(input);

        ApplyVelocity();

        UpdateSteering(input.x);

        StabilizeKart();

        ApplyGroundAdhesion();
    }

    private void UpdateState()
    {
        if (driftSystem.IsDrifting)
        {
            currentState = driftSystem.CurrentDirection == DriftSystem.DriftDirection.Right
                ? KartState.DriftingRight
                : KartState.DriftingLeft;
            return;
        }

        if (driftSystem.CurrentPhase == DriftSystem.DriftPhase.Boosting)
        {
            currentState = KartState.Boost;
            return;
        }

        bool isGrounded = groundDetector.IsGrounded;
        bool wasAirborne = currentState == KartState.Airborne;

        if (isGrounded)
        {
            if (wasAirborne) airborneSystem.ExitAirborne();
            currentState = KartState.Grounded;
        }
        else
        {
            if (!wasAirborne) airborneSystem.EnterAirborne(rb.linearVelocity);
            currentState = KartState.Airborne;
        }
    }

    private void HandleCurrentState(Vector2 input)
    {
        switch (currentState)
        {
            case KartState.DriftingLeft:
            case KartState.DriftingRight:
                // Real speed cap while drifting (a bit below top speed), not a scaled input hack.
                movementSystem.UpdateMovement(input.y, coinCount, kartConfig.DriftMaxSpeedFactor);
                break;

            case KartState.Airborne:
                // No acceleration in air, just momentum
                airborneSystem.UpdateAirborne(Time.fixedDeltaTime, input.x);
                break;

            case KartState.Grounded:
            case KartState.Boost:
            case KartState.Brake:
            case KartState.Reverse:
            case KartState.JumpHop:
            case KartState.CollisionRecovery:
            default:
                // Boost's speed bonus is applied inside MovementSystem itself, so this is
                // identical to normal grounded movement from KartController's point of view.
                movementSystem.UpdateMovement(input.y, coinCount);
                break;
        }
    }

    private void UpdateSteering(float steerInput)
    {
        if (driftSystem.IsDrifting)
        {
            int sign = driftSystem.CurrentDirection == DriftSystem.DriftDirection.Right ? 1 : -1;
            steeringSystem.UpdateDriftHeading(sign, steerInput, kartConfig.DriftTurnRate, kartConfig.DriftAngleNudgeRange);
        }
        else if (currentState != KartState.Airborne)
        {
            // While airborne, AirborneSystem already drives heading via its own reduced
            // air-control authority - letting normal grip steering run too would fight it.
            steeringSystem.UpdateSteering(steerInput);
        }
    }

    private void ApplyVelocity()
    {
        Vector3 velocity;

        switch (currentState)
        {
            case KartState.Airborne:
                velocity = airborneSystem.GetAirborneVelocity(transform);
                break;

            case KartState.DriftingLeft:
            case KartState.DriftingRight:
                // During drift, velocity follows the slide direction, not the heading
                velocity = driftSystem.GetDriftVelocityDirection() * movementSystem.CurrentSpeed;
                break;

            default:
                // Boost's speed bonus already lives inside movementSystem.CurrentSpeed,
                // so Boost/Grounded/etc. all share this normal forward-velocity path.
                velocity = movementSystem.GetVelocity(transform);
                break;
        }

        // Preserve vertical velocity from rigidbody (gravity)
        velocity.y = rb.linearVelocity.y;

        // Apply velocity
        rb.linearVelocity = velocity;
    }

    private void StabilizeKart()
    {
        // Dampen angular velocity on roll and pitch axes
        Vector3 currentAngular = rb.angularVelocity;
        rb.angularVelocity = new Vector3(
            currentAngular.x * velocityDampenFactor,
            currentAngular.y,
            currentAngular.z * velocityDampenFactor
        );

        // Apply corrective torque to keep mostly upright
        Vector3 localRotation = transform.localEulerAngles;
        float rollAngle = NormalizeAngle(localRotation.x);
        float pitchAngle = NormalizeAngle(localRotation.z);

        rb.AddRelativeTorque(-rollAngle * stabilityStrength, 0, -pitchAngle * stabilityStrength);
    }

    private void ApplyGroundAdhesion()
    {
        if (!groundDetector.IsGrounded)
            return;

        // Get surface normal
        Vector3 groundNormal = groundDetector.GroundNormal;
        Vector3 currentUp = transform.up;

        // Smoothly rotate kart to align with ground normal
        Quaternion targetRotation = Quaternion.FromToRotation(currentUp, groundNormal) * transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * groundAdhesionSpeed);
    }

    private float NormalizeAngle(float angle)
    {
        const float FullRotation = 360f;
        const float HalfRotation = 180f;

        while (angle > HalfRotation) angle -= FullRotation;
        while (angle < -HalfRotation) angle += FullRotation;
        return angle;
    }

    /// <summary>
    /// Add coins to the kart (used by gameplay systems).
    /// </summary>
    public void AddCoins(int amount)
    {
        coinCount = Mathf.Clamp(coinCount + amount, 0, 10);
    }
}

