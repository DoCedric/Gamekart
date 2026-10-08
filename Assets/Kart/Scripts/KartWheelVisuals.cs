using UnityEngine;

/// <summary>
/// Purely cosmetic wheel animation: spins all four wheels by the kart's
/// forward speed and swivels the front pair toward the current steering
/// aim. The controller itself has no WheelColliders or per-wheel physics -
/// it drives a single rolling sphere (see the design spec in Assets/Design)
/// - so without this the wheels would just sit frozen while the kart
/// corners and drifts.
///
/// Expects each wheel mesh's own local Y axis to be its axle/roll axis
/// (Unity's primitive Cylinder is already built this way). If you drop in
/// your own wheel mesh with a different axis convention, fix it by rotating
/// the mesh inside its own child transform rather than changing this script.
/// </summary>
public class KartWheelVisuals : MonoBehaviour
{
    [SerializeField] KartMotor motor;
    [SerializeField] KartAimPoint aimPoint;

    [Header("Front wheels (steer + spin)")]
    [Tooltip("Empty pivot at the wheel's steering axis. Keep its resting local rotation at identity - this script overwrites it every frame.")]
    [SerializeField] Transform frontLeftSteerPivot;
    [SerializeField] Transform frontLeftWheelMesh;
    [SerializeField] Transform frontRightSteerPivot;
    [SerializeField] Transform frontRightWheelMesh;

    [Header("Rear wheels (spin only)")]
    [SerializeField] Transform rearLeftWheelMesh;
    [SerializeField] Transform rearRightWheelMesh;

    [Header("Tuning")]
    [Tooltip("Wheel radius in meters, used to convert forward speed into a spin rate.")]
    public float wheelRadius = 0.35f;

    [Tooltip("Clamps how far the front wheels visually turn. Independent of the aim yaw's own range, so it can be tuned purely for looks.")]
    public float maxWheelTurnDegrees = 30f;

    void Update()
    {
        SteerFrontWheels();
        SpinWheel(frontLeftWheelMesh);
        SpinWheel(frontRightWheelMesh);
        SpinWheel(rearLeftWheelMesh);
        SpinWheel(rearRightWheelMesh);
    }

    void SteerFrontWheels()
    {
        float turnDegrees = Mathf.Clamp(aimPoint.AimYawDegrees, -maxWheelTurnDegrees, maxWheelTurnDegrees);
        Quaternion steerRotation = Quaternion.Euler(0f, turnDegrees, 0f);

        if (frontLeftSteerPivot != null) frontLeftSteerPivot.localRotation = steerRotation;
        if (frontRightSteerPivot != null) frontRightSteerPivot.localRotation = steerRotation;
    }

    void SpinWheel(Transform wheelMesh)
    {
        if (wheelMesh == null) return;

        float spinDegreesPerSecond = (motor.ForwardSpeed / wheelRadius) * Mathf.Rad2Deg;
        wheelMesh.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.Self);
    }
}
