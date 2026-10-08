using UnityEngine;

/// <summary>
/// Simple third-person follow camera: lerps toward a position behind/above
/// the target and looks at it. No collision handling or springarm-style
/// occlusion avoidance - just enough to see the kart drive and drift.
/// </summary>
public class KartCameraFollow : MonoBehaviour
{
    [Tooltip("The kart transform to follow.")]
    public Transform target;

    [Tooltip("Desired offset from the target, in the target's local space (e.g. behind and above). Kept fairly high and far back so the aim reticle and road ahead stay on screen.")]
    public Vector3 offset = new Vector3(0f, 4.5f, -9f);

    [Tooltip("Higher = camera catches up to the target position faster.")]
    public float positionFollowSpeed = 6f;

    [Tooltip("Higher = camera rotates to look at the target faster.")]
    public float lookFollowSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionFollowSpeed * Time.deltaTime);

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, lookFollowSpeed * Time.deltaTime);
    }
}
