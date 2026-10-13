using UnityEngine;

/// <summary>
/// Simple third-person follow camera: lerps toward a position behind/above
/// the target and looks at it. No collision handling or springarm-style
/// occlusion avoidance - just enough to see the kart drive and drift.
/// </summary>
public class KartCameraFollow : MonoBehaviour
{
    // Assigned in code by KartSpawnPoint when the player's kart spawns.
    Transform target;

    public void SetTarget(Transform newTarget) => target = newTarget;

    [Tooltip("Desired offset from the target, in the target's local space (e.g. behind and above). Kept fairly high and far back so the aim reticle and road ahead stay on screen.")]
    public Vector3 offset = new Vector3(0f, 4.5f, -9f);

    [Tooltip("Offset of the point the camera looks at, in the target's local space. (0, 2, 0) aims above the kart, so the kart sits lower on screen.")]
    public Vector3 lookAtOffset = Vector3.zero;

    [Tooltip("Higher = camera catches up to the target position faster.")]
    public float positionFollowSpeed = 6f;

    [Tooltip("Higher = camera rotates to look at the target faster.")]
    public float lookFollowSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionFollowSpeed * Time.deltaTime);

        Vector3 lookAtPoint = target.TransformPoint(lookAtOffset);
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, lookFollowSpeed * Time.deltaTime);
    }
}
