using UnityEngine;

[CreateAssetMenu(fileName = "KartTuning", menuName = "Kart/Kart Tuning")]
public class KartTuning : ScriptableObject
{
    [Header("Speed (meters per second)")]
    public float maxForwardSpeed = 24f;
    public float maxReverseSpeed = 8f;
    public float reverseEngageSpeed = 0.5f;

    [Header("Acceleration (meters per second squared)")]
    public float forwardAcceleration = 14f;
    public AnimationCurve accelerationMultiplierBySpeedFraction = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
    public float reverseAcceleration = 8f;
    public float brakeDeceleration = 30f;
    public float coastDeceleration = 4f;

    [Header("Aim ray")]
    public float aimRayOriginHeight = 2.2f;
    public float maxAimYawDegreesAtLowSpeed = 40f;
    public float maxAimYawDegreesAtTopSpeed = 18f;
    public float aimRayPitchDownDegreesAtLowSpeed = 35f;
    public float aimRayPitchDownDegreesAtTopSpeed = 12f;
    public float aimYawResponsePerSecond = 10f;
    public float steerResponseExponent = 1.5f;
    public float steerSensitivity = 1f;
    public float maxAimRayDistance = 60f;
    public float missFallbackDistance = 8f;
    public float minLookaheadDistance = 1.5f;

    [Header("Turning and grip")]
    public float maxTurnRateDegreesPerSecond = 130f;
    public float sidewaysGripPerSecond = 8f;

    [Header("Ground")]
    public LayerMask trackLayers;
    public float groundCheckExtraDistance = 0.3f;
    public float extraDownwardAcceleration = 15f;
    public float modelAlignToGroundPerSecond = 12f;

    [Header("Surfaces")]
    [Tooltip("How quickly the kart's handling blends to a new surface. Higher = snappier.")]
    public float surfaceBlendPerSecond = 8f;
}
