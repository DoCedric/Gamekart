using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Puts a kart back on the track at the last checkpoint it passed (or its starting spot).
/// Anyone can ask for it: the AI when it is stuck, later perhaps a button for the player.
///
/// Hooks for animation, effects and sound:
///  - RespawnStarted: the kart is frozen and about to disappear. FromPosition and ToPosition are set.
///    The respawn then waits Respawn Delay seconds, which is the room for a vanish animation.
///  - Respawned: the kart is at its new spot and driving again.
/// Subscribe per kart through the instance events, or to every kart at once through the static
/// Any* events (e.g. a scene object that plays a sound or particles for any respawn).
/// </summary>
[RequireComponent(typeof(KartMotor))]
public class KartRespawner : MonoBehaviour
{
    [Tooltip("Seconds the kart stays frozen before it is moved. Time for a vanish animation or sound.")]
    [SerializeField, Min(0f)] float respawnDelay = 0.5f;

    public event Action<KartRespawner> RespawnStarted;
    public event Action<KartRespawner> Respawned;
    public static event Action<KartRespawner> AnyRespawnStarted;
    public static event Action<KartRespawner> AnyRespawned;

    public bool IsRespawning { get; private set; }
    public Racer Racer { get; private set; }
    public Vector3 FromPosition { get; private set; }
    public Vector3 ToPosition { get; private set; }
    public Quaternion ToRotation { get; private set; }

    KartMotor motor;
    Rigidbody sphereBody;
    Transform kartModel;
    RaceManager raceManager;

    void Awake()
    {
        motor = GetComponent<KartMotor>();
        Racer = GetComponent<Racer>();
        sphereBody = GetComponentInChildren<Rigidbody>();
        kartModel = KartDrivingState.FindCameraTarget(gameObject);
    }

    void Start() => raceManager = FindFirstObjectByType<RaceManager>();

    /// <summary>Starts a respawn. Returns false if one is already running or the race hasn't begun.</summary>
    public bool RequestRespawn()
    {
        if (IsRespawning || raceManager == null || Racer == null || raceManager.State == RaceState.Countdown) return false;

        RacerState state = raceManager.GetState(Racer);
        if (state == null) return false;

        StartCoroutine(RespawnRoutine(state));
        return true;
    }

    IEnumerator RespawnRoutine(RacerState state)
    {
        IsRespawning = true;

        FromPosition = sphereBody.position;
        raceManager.GetRespawnPose(state, out Vector3 gatePosition, out Quaternion gateRotation);
        SnapToGround(gatePosition, gateRotation);

        // Frozen (kinematic, no input) while the effects play.
        KartDrivingState.SetDrivingEnabled(gameObject, false);
        RespawnStarted?.Invoke(this);
        AnyRespawnStarted?.Invoke(this);

        if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);
        else yield return null;

        sphereBody.transform.SetPositionAndRotation(ToPosition, ToRotation);
        kartModel.rotation = ToRotation;
        Physics.SyncTransforms();

        KartDrivingState.SetDrivingEnabled(gameObject, true);
        sphereBody.linearVelocity = Vector3.zero;
        sphereBody.angularVelocity = Vector3.zero;

        IsRespawning = false;
        Respawned?.Invoke(this);
        AnyRespawned?.Invoke(this);
    }

    // The gate pose only has an approximate height, so drop the kart onto the road below it.
    void SnapToGround(Vector3 position, Quaternion rotation)
    {
        float radius = sphereBody.GetComponent<SphereCollider>().radius;
        ToPosition = position;
        ToRotation = rotation;

        if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 30f,
                            motor.Tuning.trackLayers, QueryTriggerInteraction.Ignore))
        {
            ToPosition = hit.point + hit.normal * radius;
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, hit.normal);
            ToRotation = Quaternion.LookRotation(forward, hit.normal);
        }
    }
}
