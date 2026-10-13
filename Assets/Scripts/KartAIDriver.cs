using UnityEngine;

/// <summary>
/// Drives a kart without a player: produces the same three input values the
/// KartInputReader does, so the motor, surfaces and tuning behave identically.
/// It steers toward the gate of the checkpoint the race manager says is next, leaning
/// toward the gate after it, and slows down for sharp corners. All behaviour values come
/// from an AiDriverSettings asset, handed over by the AiSpawner.
/// </summary>
[RequireComponent(typeof(KartMotor))]
public class KartAIDriver : KartInputSource
{
    float steerInput;
    float accelerateInput;
    float brakeReverseInput;

    AiDriverSettings settings;
    KartMotor motor;
    KartRespawner respawner;
    Rigidbody sphereBody;
    Transform kartModel;
    Racer racer;
    RaceManager raceManager;
    RacerState racerState;
    float lateralOffset;     // personal line across the gates
    float startLane;         // grid lane across the finish gate, picked on the grid
    float speedFactor;
    float stuckTimer;
    Vector3 targetPoint;
    bool hasTarget;

    public override float SteerInput => steerInput;
    public override float AccelerateInput => accelerateInput;
    public override float BrakeReverseInput => brakeReverseInput;

    /// <summary>Call right after adding the component, before the kart wakes up.</summary>
    public void Init(AiDriverSettings driverSettings) => settings = driverSettings;

    void Awake()
    {
        if (settings == null) settings = ScriptableObject.CreateInstance<AiDriverSettings>();

        motor = GetComponent<KartMotor>();
        respawner = GetComponent<KartRespawner>();
        sphereBody = GetComponentInChildren<Rigidbody>();
        kartModel = KartDrivingState.FindCameraTarget(gameObject);
        racer = GetComponent<Racer>();

        motor.TopSpeedScale = settings.topSpeedScale;
        motor.AccelerationScale = settings.accelerationScale;
        lateralOffset = Random.Range(-settings.maxLateralOffset, settings.maxLateralOffset);
        speedFactor = 1f - Random.Range(0f, settings.speedVariation);
    }

    void Start() => raceManager = FindFirstObjectByType<RaceManager>();

    void Update()
    {
        hasTarget = false;
        steerInput = accelerateInput = brakeReverseInput = 0f;

        // No race-state check for "finished": a kart that has finished just keeps lapping.
        if (!TryResolveRaceState() || raceManager.State == RaceState.Countdown) return;
        if (respawner != null && respawner.IsRespawning) return;

        var checkpoints = raceManager.Checkpoints;
        Vector3 position = sphereBody.position;
        int indexA = racerState.NextCheckpoint;
        int indexB = (indexA + 1) % checkpoints.Count;
        Vector3 pointA = checkpoints[indexA].GetPointAcross(LateralOffsetFor(indexA));
        Vector3 pointB = checkpoints[indexB].GetPointAcross(LateralOffsetFor(indexB));

        Vector3 toA = Flat(pointA - position);
        float distanceToA = toA.magnitude;

        // Lean toward the gate after next as the kart closes in, for a smoother line.
        float lookAhead = Mathf.Max(settings.minLookAheadMeters, Mathf.Abs(motor.ForwardSpeed) * settings.lookAheadSeconds);
        float blend = (1f - Mathf.Clamp01(distanceToA / lookAhead)) * settings.maxLookAheadBlend;
        targetPoint = Vector3.Lerp(pointA, pointB, blend);
        hasTarget = true;

        steerInput = SteerToward(targetPoint);
        DriveAtSpeed(TargetSpeed(toA, Flat(pointB - pointA), distanceToA));
        CheckIfStuck();
    }

    // Slower for sharp corners (the more so the closer they are) and while steering hard.
    float TargetSpeed(Vector3 toA, Vector3 gateAToB, float distanceToA)
    {
        float topSpeed = motor.MaxForwardSpeed;
        float straightSpeed = topSpeed * settings.straightSpeedFraction * speedFactor;
        float cornerSpeed = topSpeed * settings.cornerSpeedFraction;

        float cornerSharpness = gateAToB.sqrMagnitude > 0.01f
            ? Mathf.InverseLerp(10f, settings.sharpCornerAngle, Vector3.Angle(toA, gateAToB))
            : 0f;
        float proximity = 1f - Mathf.Clamp01((distanceToA - 5f) / settings.brakeLookAheadMeters);

        float speed = Mathf.Lerp(straightSpeed, cornerSpeed, cornerSharpness * proximity);
        return speed * (1f - settings.steerSlowdown * Mathf.Abs(steerInput));
    }

    void DriveAtSpeed(float targetSpeed)
    {
        float speedError = targetSpeed - motor.ForwardSpeed;
        accelerateInput = Mathf.Clamp01(speedError / 2f);
        brakeReverseInput = Mathf.Clamp01((-speedError - 1f) / 4f); // only brakes when clearly too fast
    }

    // Not moving for a while while the race is on: ask for a respawn at the last checkpoint.
    void CheckIfStuck()
    {
        if (respawner == null || raceManager.State != RaceState.Racing || raceManager.RaceTime < settings.stuckGraceSeconds)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer = Mathf.Abs(motor.ForwardSpeed) < settings.stuckSpeed ? stuckTimer + Time.deltaTime : 0f;
        if (stuckTimer >= settings.stuckSeconds)
        {
            stuckTimer = 0f;
            respawner.RequestRespawn();
        }
    }

    bool TryResolveRaceState()
    {
        if (racerState != null) return true;
        if (raceManager == null || racer == null) return false;
        racerState = raceManager.GetState(racer);
        if (racerState == null) return false;

        // Still sitting on the grid: remember which lane of the finish gate this kart starts in.
        float lane = raceManager.Checkpoints[0].GetLateralPosition(sphereBody.position)
                     + Random.Range(-settings.startLaneDeviation, settings.startLaneDeviation);
        startLane = Mathf.Clamp(lane, -0.9f, 0.9f);
        return true;
    }

    // Opening stretch: follow the grid lane, then ease onto this AI's personal line.
    float LateralOffsetFor(int gateIndex)
    {
        if (racerState.CurrentLap >= 2) return lateralOffset;
        if (gateIndex == 0) return racerState.CurrentLap == 0 ? startLane : lateralOffset; // 0 = start line, then the lap-1 finish
        float settled = Mathf.Clamp01(gateIndex / (float)settings.gatesToSettleOnLine);
        return Mathf.Lerp(startLane, lateralOffset, settled);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Works out the raw steer value that makes the kart's aim point land on the target.
    float SteerToward(Vector3 target)
    {
        KartTuning tuning = motor.Tuning;
        Vector3 up = kartModel.up;
        Vector3 heading = Vector3.ProjectOnPlane(kartModel.forward, up);
        Vector3 toTarget = Vector3.ProjectOnPlane(target - sphereBody.position, up);
        if (toTarget.sqrMagnitude < 0.0001f) return 0f;

        float angle = Vector3.SignedAngle(heading, toTarget, up); // positive = target is to the right

        // Steering sets an aim yaw of steer * maxAimYaw, so the wanted fraction of that range is:
        float maxAimYaw = Mathf.Lerp(tuning.maxAimYawDegreesAtLowSpeed, tuning.maxAimYawDegreesAtTopSpeed, motor.SpeedFraction);
        float wantedSteer = Mathf.Clamp(angle / maxAimYaw, -1f, 1f);

        // KartAimPoint shapes the raw input (power curve and sensitivity); undo that.
        float unscaled = Mathf.Abs(wantedSteer) / Mathf.Max(tuning.steerSensitivity, 0.01f);
        return Mathf.Sign(wantedSteer) * Mathf.Clamp01(Mathf.Pow(unscaled, 1f / tuning.steerResponseExponent));
    }

    void OnDrawGizmos()
    {
        if (!hasTarget || sphereBody == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(sphereBody.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, 0.5f);
    }
}
