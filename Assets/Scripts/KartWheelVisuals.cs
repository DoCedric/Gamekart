using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Purely cosmetic wheel animation: spins all four wheels by the kart's
/// forward speed and swivels the front pair toward the current steering
/// aim. The controller itself has no WheelColliders or per-wheel physics -
/// it drives a single rolling sphere (see the design spec in Assets/Design)
/// - so without this the wheels would just sit frozen while the kart
/// corners and drifts.
///
/// Wheels are owned by this script, not by the Animator: it runs in
/// LateUpdate (after the Animator) and writes every wheel's rotation from
/// its rest pose, so keys on wheel bones in an animation clip are ignored.
///
/// Expects each wheel's local Y axis to be its axle (a Blender bone's Y
/// axis runs along the bone, so a wheel bone pointing sideways out of the
/// hub is exactly this). Which way along the axle it points doesn't matter:
/// left and right wheels may mirror each other. Bone roll doesn't matter either.
/// Front wheels steer around the kart's up axis through their own origin.
/// </summary>
public class KartWheelVisuals : MonoBehaviour
{
    [SerializeField] KartMotor motor;
    [SerializeField] KartAimPoint aimPoint;

    [Header("Front wheels (steer + spin)")]
    [FormerlySerializedAs("frontLeftWheelMesh")]
    [SerializeField] Transform frontLeftWheel;
    [FormerlySerializedAs("frontRightWheelMesh")]
    [SerializeField] Transform frontRightWheel;

    [Header("Rear wheels (spin only)")]
    [FormerlySerializedAs("rearLeftWheelMesh")]
    [SerializeField] Transform rearLeftWheel;
    [FormerlySerializedAs("rearRightWheelMesh")]
    [SerializeField] Transform rearRightWheel;

    [Header("Tuning")]
    [Tooltip("Wheel radius in meters, used to convert forward speed into a spin rate.")]
    public float wheelRadius = 0.35f;

    [Tooltip("Clamps how far the front wheels visually turn. Independent of the aim yaw's own range, so it can be tuned purely for looks.")]
    public float maxWheelTurnDegrees = 30f;

    // One per wheel. Rest pose and axes are read once in Awake,
    // while the kart model still sits upright under the root.
    class Wheel
    {
        public Transform transform;
        public Quaternion restLocalRotation;
        public Vector3 steerAxisInParent; // the kart's up axis, in the parent's space
        public float spinSign;            // +1 if local Y points to the kart's right, -1 if to its left
    }

    Wheel frontLeft, frontRight, rearLeft, rearRight;
    float spinDegrees;

    void Awake()
    {
        frontLeft = Capture(frontLeftWheel);
        frontRight = Capture(frontRightWheel);
        rearLeft = Capture(rearLeftWheel);
        rearRight = Capture(rearRightWheel);
    }

    Wheel Capture(Transform target)
    {
        if (target == null) return null;

        Transform parent = target.parent;
        Vector3 kartUp = transform.up;
        Vector3 axleWorld = target.TransformDirection(Vector3.up);

        return new Wheel
        {
            transform = target,
            restLocalRotation = target.localRotation,
            steerAxisInParent = parent != null ? parent.InverseTransformDirection(kartUp) : kartUp,
            spinSign = Vector3.Dot(axleWorld, transform.right) >= 0f ? 1f : -1f,
        };
    }

    void LateUpdate()
    {
        float spinDegreesPerMeter = Mathf.Rad2Deg / Mathf.Max(wheelRadius, 0.01f);
        spinDegrees = Mathf.Repeat(spinDegrees + motor.ForwardSpeed * spinDegreesPerMeter * Time.deltaTime, 360f);

        float steerDegrees = Mathf.Clamp(aimPoint.AimYawDegrees, -maxWheelTurnDegrees, maxWheelTurnDegrees);

        Pose(frontLeft, steerDegrees);
        Pose(frontRight, steerDegrees);
        Pose(rearLeft, 0f);
        Pose(rearRight, 0f);
    }

    void Pose(Wheel wheel, float steerDegrees)
    {
        if (wheel == null) return;

        // Spinning around +local Y by +angle rolls forward when Y points right; mirrored wheels flip the sign.
        Quaternion spin = Quaternion.AngleAxis(spinDegrees * wheel.spinSign, Vector3.up);
        Quaternion steer = Quaternion.AngleAxis(steerDegrees, wheel.steerAxisInParent);
        wheel.transform.localRotation = steer * wheel.restLocalRotation * spin;
    }
}
