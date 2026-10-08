using UnityEngine;

[RequireComponent(typeof(KartInputReader))]
public class KartAimPoint : MonoBehaviour
{
    [SerializeField] KartTuning tuning;
    [SerializeField] Rigidbody sphereBody;
    [SerializeField] Transform kartModel;

    public Vector3 AimPointWorld { get; private set; }
    public bool AimRayHitTrack { get; private set; }
    public float AimYawDegrees { get; private set; }

    KartInputReader inputReader;

    void Awake() => inputReader = GetComponent<KartInputReader>();

    // Called by KartMotor once per FixedUpdate, before steering.
    public void UpdateAim(float speedFraction, float deltaTime)
    {
        float maxAimYaw = Mathf.Lerp(tuning.maxAimYawDegreesAtLowSpeed, tuning.maxAimYawDegreesAtTopSpeed, speedFraction);
        float rawSteer = inputReader.SteerInput;
        float shapedSteer = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), tuning.steerResponseExponent);
        float steer = Mathf.Clamp(shapedSteer * tuning.steerSensitivity, -1f, 1f);
        float targetAimYaw = steer * maxAimYaw;
        float smoothing = 1f - Mathf.Exp(-tuning.aimYawResponsePerSecond * deltaTime);
        AimYawDegrees = Mathf.Lerp(AimYawDegrees, targetAimYaw, smoothing);

        float pitchDownDegrees = Mathf.Lerp(tuning.aimRayPitchDownDegreesAtLowSpeed, tuning.aimRayPitchDownDegreesAtTopSpeed, speedFraction);

        Vector3 up = kartModel.up;
        Vector3 right = kartModel.right;
        Vector3 forward = kartModel.forward;
        Quaternion yawRotation = Quaternion.AngleAxis(AimYawDegrees, up);

        Vector3 rayOrigin = sphereBody.position + up * tuning.aimRayOriginHeight;
        Vector3 rayDirection = yawRotation * (Quaternion.AngleAxis(pitchDownDegrees, right) * forward);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, tuning.maxAimRayDistance,
                            tuning.trackLayers, QueryTriggerInteraction.Ignore))
        {
            AimPointWorld = hit.point;
            AimRayHitTrack = true;
        }
        else
        {
            AimPointWorld = sphereBody.position + yawRotation * forward * tuning.missFallbackDistance;
            AimRayHitTrack = false;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || sphereBody == null) return;
        Gizmos.color = AimRayHitTrack ? Color.green : Color.red;
        Gizmos.DrawLine(sphereBody.position, AimPointWorld);
        Gizmos.DrawSphere(AimPointWorld, 0.3f);
    }
}
