using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feeds the kart's driving state into an Animator, so a kart's animations
/// can be built entirely in its Animator Controller. Put it on the kart root
/// next to the KartMotor; the Animator goes on the model under KartModel
/// (never on KartModel itself - the motor moves that one) with Apply Root
/// Motion off.
///
/// Parameters it sets, when the controller has them (all are optional, use
/// the ones you need; names and types must match exactly):
///  - float   Speed          signed forward speed in m/s, negative when reversing
///  - float   SpeedFraction  0 standing still, 1 at top speed (above 1 while boosting)
///  - float   Steer          -1 full left, +1 full right (smoothed)
///  - bool    IsDriving      false on the selection screen and while held at the start
///  - bool    IsGrounded     false while the kart is in the air
///  - bool    IsBoosting     true while a boost pad lifts the kart over its top speed
///  - trigger Respawn        fired when the kart starts a respawn
///  - trigger Respawned      fired when the kart is back on the track
///
/// Wheels are not animated through here: KartWheelVisuals spins and steers them.
/// </summary>
public class KartAnimatorParameters : MonoBehaviour
{
    [Tooltip("Leave empty to use the first Animator under this kart.")]
    [SerializeField] Animator animator;

    [Tooltip("Seconds Speed, SpeedFraction and Steer take to follow a change. Higher is smoother.")]
    [SerializeField, Min(0f)] float dampTime = 0.15f;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int SpeedFractionId = Animator.StringToHash("SpeedFraction");
    static readonly int SteerId = Animator.StringToHash("Steer");
    static readonly int IsDrivingId = Animator.StringToHash("IsDriving");
    static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");
    static readonly int IsBoostingId = Animator.StringToHash("IsBoosting");
    static readonly int RespawnId = Animator.StringToHash("Respawn");
    static readonly int RespawnedId = Animator.StringToHash("Respawned");

    KartMotor motor;
    KartInputSource input;
    KartRespawner respawner;
    readonly HashSet<int> availableParameters = new HashSet<int>();

    void Awake()
    {
        motor = GetComponent<KartMotor>();
        input = GetComponent<KartInputSource>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Setting a parameter the controller doesn't have logs a warning every frame, so only set the ones it has.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                availableParameters.Add(parameter.nameHash);
        }
    }

    // Start, not Awake: the spawners add the respawner right after instantiating the kart.
    void Start()
    {
        respawner = GetComponent<KartRespawner>();
        if (respawner != null)
        {
            respawner.RespawnStarted += OnRespawnStarted;
            respawner.Respawned += OnRespawned;
        }
    }

    void OnDestroy()
    {
        if (respawner != null)
        {
            respawner.RespawnStarted -= OnRespawnStarted;
            respawner.Respawned -= OnRespawned;
        }
    }

    void Update()
    {
        if (animator == null || motor == null) return;

        bool driving = motor.enabled;
        float speed = driving ? motor.ForwardSpeed : 0f;
        float speedFraction = driving ? Mathf.Abs(speed) / Mathf.Max(motor.MaxForwardSpeed, 0.01f) : 0f;
        float steer = driving && input != null && input.enabled ? input.SteerInput : 0f;

        SetFloat(SpeedId, speed);
        SetFloat(SpeedFractionId, speedFraction);
        SetFloat(SteerId, steer);
        SetBool(IsDrivingId, driving);
        SetBool(IsGroundedId, !driving || motor.IsGrounded);
        SetBool(IsBoostingId, driving && motor.IsBoosting);
    }

    void SetFloat(int id, float value)
    {
        if (availableParameters.Contains(id)) animator.SetFloat(id, value, dampTime, Time.deltaTime);
    }

    void SetBool(int id, bool value)
    {
        if (availableParameters.Contains(id)) animator.SetBool(id, value);
    }

    void OnRespawnStarted(KartRespawner _)
    {
        if (availableParameters.Contains(RespawnId)) animator.SetTrigger(RespawnId);
    }

    void OnRespawned(KartRespawner _)
    {
        if (availableParameters.Contains(RespawnedId)) animator.SetTrigger(RespawnedId);
    }
}
