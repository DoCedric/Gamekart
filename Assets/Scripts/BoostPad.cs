using System;
using UnityEngine;

/// <summary>
/// An invisible boost zone. Any kart that drives into the box is boosted up to the given
/// percentage of its top speed, then slows back down by itself (the rate is set on the kart's
/// tuning). Place it on the road and size the box; it has no renderer, so add visuals separately.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BoostPad : MonoBehaviour
{
    [Tooltip("Speed the kart is boosted to, as a percentage of its top speed. 100 = top speed, 150 = one and a half times top speed. A kart already faster than this is not slowed down.")]
    [SerializeField, Min(0f)] float speedPercent = 100f;

    /// <summary>Raised when a kart is boosted. Hook sounds and effects here.</summary>
    public event Action<BoostPad, KartMotor> Triggered;

    void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 1f, 6f);
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;

        KartMotor motor = body.GetComponentInParent<KartMotor>();
        if (motor == null) return;

        motor.ApplyBoost(speedPercent / 100f);
        Triggered?.Invoke(this, motor);
    }

#if UNITY_EDITOR
    void OnDrawGizmos() => DrawBox(new Color(1f, 0.55f, 0f, 0.2f), new Color(1f, 0.55f, 0f, 1f));
    void OnDrawGizmosSelected() => DrawBox(new Color(1f, 0.8f, 0.2f, 0.35f), new Color(1f, 0.8f, 0.2f, 1f));

    void DrawBox(Color fill, Color line)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = fill;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = line;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;

        UnityEditor.Handles.Label(transform.TransformPoint(box.center + Vector3.up * (box.size.y * 0.5f + 0.5f)),
                                  $"BOOST {speedPercent:0}%");
    }
#endif
}
