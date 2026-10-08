using UnityEngine;

[RequireComponent(typeof(KartInputReader), typeof(KartAimPoint))]
public class KartMotor : MonoBehaviour
{
    [SerializeField] KartTuning tuning;
    [SerializeField] Rigidbody sphereBody;
    [SerializeField] Transform kartModel;
    [SerializeField] float sphereRadius = 0.5f;
    [SerializeField] float modelHeightBelowSphereCenter = 0.5f;

    [Tooltip("Used when the ground under the kart has no SurfaceZone. Leave empty for neutral handling.")]
    [SerializeField] SurfaceType defaultSurface;

    public float ForwardSpeed { get; private set; } // signed meters per second along the heading
    public SurfaceType CurrentSurface { get; private set; }

    // Blended toward the current surface so handling doesn't snap at surface edges.
    float maxSpeedMultiplier = 1f;
    float accelerationMultiplier = 1f;
    float gripMultiplier = 1f;

    KartInputReader inputReader;
    KartAimPoint aimPoint;
    bool isGrounded;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        inputReader = GetComponent<KartInputReader>();
        aimPoint = GetComponent<KartAimPoint>();
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        CheckGround();
        BlendSurfaceMultipliers(deltaTime);

        float speedFraction = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / (tuning.maxForwardSpeed * maxSpeedMultiplier));
        aimPoint.UpdateAim(speedFraction, deltaTime);
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
        float maxForwardSpeed = tuning.maxForwardSpeed * maxSpeedMultiplier;
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
                float speedFraction = Mathf.Clamp01(speed / maxForwardSpeed);
                float curve = tuning.accelerationMultiplierBySpeedFraction.Evaluate(speedFraction);
                speed += tuning.forwardAcceleration * accelerationMultiplier * curve * accelerate * deltaTime;
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
