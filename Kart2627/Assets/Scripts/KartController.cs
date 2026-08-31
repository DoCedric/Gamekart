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
        bool driftInput = driftAction != null && driftAction.action.IsPressed();

        // Update state
        UpdateState(input, driftInput);

        // Handle state-specific logic
        HandleCurrentState(input, driftInput);

        // Apply velocity to rigidbody based on state
        ApplyVelocity();

        // Update steering
        steeringSystem.UpdateSteering(input.x);

        // Stabilize kart (prevent excessive rolling)
        StabilizeKart();

        // Apply ground adhesion (slope following)
        ApplyGroundAdhesion();
    }

    private void UpdateState(Vector2 input, bool driftInput)
    {
        bool isGrounded = groundDetector.IsGrounded;
        bool isDrifting = driftSystem.IsDrifting;

        // Drifting takes priority
        if (isDrifting)
        {
            if (currentState != KartState.DriftingLeft && currentState != KartState.DriftingRight)
            {
                // Determine drift direction
                currentState = driftSystem.CurrentDirection == DriftSystem.DriftDirection.Left 
                    ? KartState.DriftingLeft 
                    : KartState.DriftingRight;
            }
        }
        // Try to enter drift from grounded state
        else if (isGrounded && driftInput && currentState == KartState.Grounded)
        {
            if (driftSystem.TryEnterDrift(input.x, rb))
            {
                currentState = driftSystem.CurrentDirection == DriftSystem.DriftDirection.Left 
                    ? KartState.DriftingLeft 
                    : KartState.DriftingRight;
            }
        }
        // Exit drift
        else if ((isDrifting && !driftInput) || (isDrifting && !isGrounded))
        {
            int turboTier = driftSystem.ExitDrift();
            // TODO: Phase 4 - trigger mini-turbo with turboTier
            currentState = isGrounded ? KartState.Grounded : KartState.Airborne;
        }
        // Grounded/Airborne transitions
        else if (!isDrifting)
        {
            bool wasGrounded = currentState == KartState.Grounded;
            
            if (isGrounded && !wasGrounded)
            {
                currentState = KartState.Grounded;
                airborneSystem.ExitAirborne();
            }
            else if (!isGrounded && wasGrounded)
            {
                currentState = KartState.Airborne;
                airborneSystem.EnterAirborne(rb.linearVelocity);
            }
        }
    }

    private void HandleCurrentState(Vector2 input, bool driftInput)
    {
        switch (currentState)
        {
            case KartState.Grounded:
                movementSystem.UpdateMovement(input.y, coinCount);
                break;

            case KartState.DriftingLeft:
            case KartState.DriftingRight:
                // During drift, reduce forward movement slightly
                movementSystem.UpdateMovement(input.y * 0.8f, coinCount);
                driftSystem.UpdateDrift(input.x, Time.fixedDeltaTime);
                break;

            case KartState.Airborne:
                // No acceleration in air, just momentum
                airborneSystem.UpdateAirborne(Time.fixedDeltaTime, input.x);
                break;

            case KartState.Brake:
            case KartState.Reverse:
            case KartState.Boost:
            case KartState.JumpHop:
            case KartState.CollisionRecovery:
                // TODO: Implement in future phases
                movementSystem.UpdateMovement(input.y, coinCount);
                break;
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
                // During drift, apply movement in current heading direction
                velocity = movementSystem.GetVelocity(transform);
                break;

            default:
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

