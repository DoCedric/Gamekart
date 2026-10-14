using UnityEngine;

[RequireComponent(typeof(KartInputSource), typeof(KartAimPoint))]
public class KartMotor : MonoBehaviour
{
    [SerializeField] KartTuning tuning;
    [SerializeField] Rigidbody sphereBody;
    [SerializeField] Transform kartModel;
    [Tooltip("Must match the SphereBody's collider radius.")]
    [SerializeField] float sphereRadius = 1f;
    [Tooltip("Normally equal to the sphere radius, so the wheels sit on the road.")]
    [SerializeField] float modelHeightBelowSphereCenter = 1f;

    [Tooltip("Used when the ground under the kart has no SurfaceZone. Leave empty for neutral handling.")]
    [SerializeField] SurfaceType defaultSurface;

    public float ForwardSpeed { get; private set; } // signed meters per second along the heading
    public SurfaceType CurrentSurface { get; private set; }
    public KartTuning Tuning => tuning;

    // Per-kart scaling on top of the shared tuning asset, e.g. for AI difficulty. 1 = the kart's own handling.
    public float TopSpeedScale { get; set; } = 1f;
    public float AccelerationScale { get; set; } = 1f;

    /// <summary>Top speed on neutral ground, including the per-kart scale.</summary>
    public float MaxForwardSpeed => tuning.maxForwardSpeed * TopSpeedScale;
    public float SpeedFraction => Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / (MaxForwardSpeed * maxSpeedMultiplier));

    /// <summary>True while a boost is letting the kart go faster than its normal top speed.</summary>
    public bool IsBoosting => boostCeiling > MaxForwardSpeed * maxSpeedMultiplier + 0.01f;

    /// <summary>
    /// Raises the kart's speed to the given fraction of its top speed (1 = top speed, 1.5 = 150%),
    /// then lets it fall back by itself. Never slows the kart down.
    /// </summary>
    public void ApplyBoost(float fractionOfTopSpeed)
    {
        float targetSpeed = fractionOfTopSpeed * MaxForwardSpeed;
        boostCeiling = Mathf.Max(boostCeiling, targetSpeed);
        pendingBoostSpeed = Mathf.Max(pendingBoostSpeed, targetSpeed); // applied on the next grounded step
    }

    // Blended toward the current surface so handling doesn't snap at surface edges.
    float maxSpeedMultiplier = 1f;
    float accelerationMultiplier = 1f;
    float gripMultiplier = 1f;

    // Boost: the speed limit is temporarily raised to boostCeiling, which then decays back down.
    float boostCeiling;
    float pendingBoostSpeed;

    KartInputSource inputReader;
    KartAimPoint aimPoint;
    bool isGrounded;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        inputReader = GetComponent<KartInputSource>();
        aimPoint = GetComponent<KartAimPoint>();
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        CheckGround();
        BlendSurfaceMultipliers(deltaTime);
        boostCeiling = Mathf.MoveTowards(boostCeiling, 0f, tuning.boostDecayPerSecond * deltaTime);

        aimPoint.UpdateAim(SpeedFraction, deltaTime);
        AlignModelToGround(deltaTime);

        if (isGrounded)
        {
            TurnTowardAimPoint(deltaTime);
            DriveAlongHeading(deltaTime);
            sphereBody.AddForce(-groundNormal * tuning.extraDownwardAcceleration, ForceMode.Acceleration);
        }
        else
        {
            sphereBody.AddForce(Vector3.down * tuning.extraDownwardAcceleration, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        kartModel.position = sphereBody.transform.position - kartModel.up * modelHeightBelowSphereCenter;
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(sphereBody.position, Vector3.down, out RaycastHit hit,
                                     sphereRadius + tuning.groundCheckExtraDistance,
                                     tuning.trackLayers, QueryTriggerInteraction.Ignore);
        groundNormal = isGrounded ? hit.normal : Vector3.up;

        if (isGrounded)
        {
            SurfaceZone zone = hit.collider.GetComponentInParent<SurfaceZone>();
            CurrentSurface = zone != null && zone.SurfaceType != null ? zone.SurfaceType : defaultSurface;
        }
    }

    void BlendSurfaceMultipliers(float deltaTime)
    {
        float targetMaxSpeed = CurrentSurface != null ? CurrentSurface.maxSpeedMultiplier : 1f;
        float targetAcceleration = CurrentSurface != null ? CurrentSurface.accelerationMultiplier : 1f;
        float targetGrip = CurrentSurface != null ? CurrentSurface.gripMultiplier : 1f;

        float blend = 1f - Mathf.Exp(-tuning.surfaceBlendPerSecond * deltaTime);
        maxSpeedMultiplier = Mathf.Lerp(maxSpeedMultiplier, targetMaxSpeed, blend);
        accelerationMultiplier = Mathf.Lerp(accelerationMultiplier, targetAcceleration, blend);
        gripMultiplier = Mathf.Lerp(gripMultiplier, targetGrip, blend);
    }

    void AlignModelToGround(float deltaTime)
    {
        Quaternion alignedRotation = Quaternion.FromToRotation(kartModel.up, groundNormal) * kartModel.rotation;
        float smoothing = 1f - Mathf.Exp(-tuning.modelAlignToGroundPerSecond * deltaTime);
        kartModel.rotation = Quaternion.Slerp(kartModel.rotation, alignedRotation, smoothing);
    }

    // Pure pursuit: curvature = 2 * sin(angleToAim) / lookaheadDistance; turnRate = speed * curvature.
    void TurnTowardAimPoint(float deltaTime)
    {
        Vector3 heading = Vector3.ProjectOnPlane(kartModel.forward, groundNormal).normalized;
        Vector3 toAimPoint = Vector3.ProjectOnPlane(aimPoint.AimPointWorld - sphereBody.position, groundNormal);

        float lookaheadDistance = Mathf.Max(toAimPoint.magnitude, tuning.minLookaheadDistance);
        float angleToAimDegrees = Vector3.SignedAngle(heading, toAimPoint, groundNormal);
        float curvaturePerMeter = 2f * Mathf.Sin(angleToAimDegrees * Mathf.Deg2Rad) / lookaheadDistance;

        float turnRateDegreesPerSecond = ForwardSpeed * curvaturePerMeter * Mathf.Rad2Deg;
        turnRateDegreesPerSecond = Mathf.Clamp(turnRateDegreesPerSecond,
            -tuning.maxTurnRateDegreesPerSecond, tuning.maxTurnRateDegreesPerSecond);

        kartModel.rotation = Quaternion.AngleAxis(turnRateDegreesPerSecond * deltaTime, groundNormal) * kartModel.rotation;
    }

    void DriveAlongHeading(float deltaTime)
    {
        Vector3 heading = Vector3.ProjectOnPlane(kartModel.forward, groundNormal).normalized;
        Vector3 rightOfHeading = Vector3.Cross(groundNormal, heading);

        Vector3 velocity = sphereBody.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, heading);
        float sidewaysSpeed = Vector3.Dot(velocity, rightOfHeading);
        Vector3 remainingVelocity = velocity - heading * forwardSpeed - rightOfHeading * sidewaysSpeed;

        if (pendingBoostSpeed > 0f)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, pendingBoostSpeed);
            pendingBoostSpeed = 0f;
        }

        forwardSpeed = ApplyThrottleAndBrake(forwardSpeed, deltaTime);
        float gripSmoothing = 1f - Mathf.Exp(-tuning.sidewaysGripPerSecond * gripMultiplier * deltaTime);
        sidewaysSpeed = Mathf.Lerp(sidewaysSpeed, 0f, gripSmoothing);

        sphereBody.linearVelocity = heading * forwardSpeed + rightOfHeading * sidewaysSpeed + remainingVelocity;
        ForwardSpeed = forwardSpeed;
    }

    float ApplyThrottleAndBrake(float speed, float deltaTime)
    {
        float accelerate = inputReader.AccelerateInput;
        float brakeReverse = inputReader.BrakeReverseInput;
        float normalMaxForwardSpeed = MaxForwardSpeed * maxSpeedMultiplier;
        float maxForwardSpeed = Mathf.Max(normalMaxForwardSpeed, boostCeiling); // a boost lifts the limit for a while
        float maxReverseSpeed = tuning.maxReverseSpeed * maxSpeedMultiplier;
        bool accelerateHeld = accelerate > 0.05f;
        bool brakeHeld = brakeReverse > 0.05f;

        if (accelerateHeld && !brakeHeld)
        {
            if (speed < 0f)
            {
                speed += tuning.brakeDeceleration * deltaTime; // rolling backward: brake first
            }
            else
            {
                float speedFraction = Mathf.Clamp01(speed / normalMaxForwardSpeed);
                float curve = tuning.accelerationMultiplierBySpeedFraction.Evaluate(speedFraction);
                speed += tuning.forwardAcceleration * AccelerationScale * accelerationMultiplier * curve * accelerate * deltaTime;
            }
        }
        else if (brakeHeld && !accelerateHeld && speed <= tuning.reverseEngageSpeed)
        {
            speed -= tuning.reverseAcceleration * accelerationMultiplier * brakeReverse * deltaTime; // reverse
        }
        else if (brakeHeld)
        {
            speed = Mathf.MoveTowards(speed, 0f, tuning.brakeDeceleration * brakeReverse * deltaTime); // brake, never reverse
        }
        else
        {
            speed = Mathf.MoveTowards(speed, 0f, tuning.coastDeceleration * deltaTime);
        }

        return Mathf.Clamp(speed, -maxReverseSpeed, maxForwardSpeed);
    }
}
