using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartSuspension : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        [Header("Transforms")]
        public Transform anchor;         // Suspension top (on chassis). +Y is suspension up.
        public Transform rest;           // Wheel center at rest ride height (optional but recommended).
        public Transform visual;         // Visual wheel mesh/GO (optional).

        [Header("Geometry")]
        public float wheelRadius = 0.17f;
        public float travel = 0.20f;     // Total stroke (m), centered around restLength
        public float restLength = 0.35f; // Auto-derived if 'rest' provided
        public bool deriveRestFromTransform = true;

        [Header("Raycast")]
        public LayerMask groundMask = ~0;

        // Runtime
        [HideInInspector] public float compression;       // +ve = compressed
        [HideInInspector] public float prevCompression;
        [HideInInspector] public bool grounded;
        [HideInInspector] public Vector3 hitPoint;
        [HideInInspector] public Vector3 axisDown;        // -anchor.up
        [HideInInspector] public Vector3 axisUp;          // anchor.up
        [HideInInspector] public float currentLen;        // current suspension length (m)
        [HideInInspector] public float minLen;
        [HideInInspector] public float maxLen;
        [HideInInspector] public float rayLen;
        [HideInInspector] public Vector3 groundNormal;
    }

    [System.Serializable]
    public class Axle
    {
        [Tooltip("Indices into the 'wheels' array for left & right wheels of this axle.")]
        public int leftIndex = 0;
        public int rightIndex = 1;
        [Tooltip("Anti-roll rate (N/m). Higher = less body roll.")]
        public float antiRollStiffness = 8000f;
    }

    [Header("Wheels (order up to you)")]
    public Wheel[] wheels;

    [Header("Axles for anti-roll")]
    public Axle[] axles = new Axle[]
    {
        new Axle { leftIndex = 0, rightIndex = 1, antiRollStiffness = 8000f }, // Front
        new Axle { leftIndex = 2, rightIndex = 3, antiRollStiffness = 8000f }, // Rear
    };

    [Header("Spring-Damper (Global)")]
    [Tooltip("Spring rate k (N/m), shared by all wheels.")]
    public float stiffness = 20000f;
    [Tooltip("Damping c (N*s/m), shared by all wheels.")]
    public float damping = 2500f;
    [Tooltip("Extra constant upward force per wheel (N).")]
    public float preload = 0f;

    [Header("Visuals")]
    public bool alignVisualToGroundNormal = true;
    public float visualLerp = 30f; // smooth the visual motion

    [Header("Gizmos")]
    public bool gizmoShowTravel = true;
    public bool gizmoShowRest = true;
    public bool gizmoShowRay = true;
    public bool gizmoShowWheelRadius = true;
    public Color gizmoTravelColor = Color.cyan;
    public Color gizmoRestColor = Color.yellow;
    public Color gizmoRayColorGrounded = Color.green;
    public Color gizmoRayColorAir = Color.red;
    public Color gizmoWheelRadiusColor = new Color(1f, 0f, 1f, 0.85f); // magenta

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        EnsureRestLengths();
    }

    private void OnValidate()
    {
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                if (w != null && w.anchor != null)
                {
                    if (w.deriveRestFromTransform && w.rest != null)
                    {
                        DeriveRestLength(i);
                    }
                    else
                    {
                        // Keep basic derived values current
                        UpdateWheelDerived(i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Derives rest lengths from anchor/rest transforms when available.
    /// </summary>
    private void EnsureRestLengths()
    {
        if (wheels == null) return;
        for (int i = 0; i < wheels.Length; i++)
            DeriveRestLength(i);
    }

    private void DeriveRestLength(int i)
    {
        if (i < 0 || i >= wheels.Length) return;
        var w = wheels[i];
        if (w == null || w.anchor == null) return;

        w.axisUp = w.anchor.up;
        w.axisDown = -w.axisUp;

        if (w.deriveRestFromTransform && w.rest != null)
        {
            // Project rest position onto axisUp from anchor to get length along the suspension axis.
            Vector3 delta = w.rest.position - w.anchor.position;
            float lengthAlongAxis = Mathf.Abs(Vector3.Dot(delta, w.axisUp)); // ensure positive length
            w.restLength = Mathf.Max(0.01f, lengthAlongAxis);
        }

        UpdateWheelDerived(i);
    }

    private void UpdateWheelDerived(int i)
    {
        var w = wheels[i];
        float half = Mathf.Max(0f, w.travel) * 0.5f;
        w.minLen = Mathf.Max(0f, w.restLength - half);
        w.maxLen = w.restLength + half;
        w.rayLen = w.maxLen + w.wheelRadius + 0.05f; // slight pad
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) Per-wheel spring-damper forces (global k, c, preload)
        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (w == null || w.anchor == null) continue;

            // Refresh axes (anchor might rotate)
            w.axisUp = w.anchor.up;
            w.axisDown = -w.axisUp;

            // Keep derived values current
            UpdateWheelDerived(i);

            // Raycast to find ground
            Ray ray = new Ray(w.anchor.position, w.axisDown);
            if (Physics.Raycast(ray, out RaycastHit hit, w.rayLen, w.groundMask, QueryTriggerInteraction.Ignore))
            {
                w.grounded = true;
                w.hitPoint = hit.point;
                w.groundNormal = hit.normal;

                // Current length from anchor to wheel center plane
                float len = hit.distance - w.wheelRadius;
                w.currentLen = Mathf.Clamp(len, w.minLen, w.maxLen);

                // Compression (+ve when compressed)
                w.prevCompression = w.compression;
                w.compression = w.restLength - w.currentLen;

                // Suspension velocity via relative velocity projected onto axisDown
                float vAnchor = Vector3.Dot(rb.GetPointVelocity(w.anchor.position), w.axisDown);
                float vGround = 0f;
                if (hit.rigidbody)
                    vGround = Vector3.Dot(hit.rigidbody.GetPointVelocity(hit.point), w.axisDown);
                float suspVel = vGround - vAnchor; // +ve when compressing

                // Spring + damper (global)
                float springForce = stiffness * w.compression;
                float damperForce = damping * suspVel;
                float F = Mathf.Max(0f, springForce + damperForce) + Mathf.Max(0f, preload);

                // Apply upward along axis
                Vector3 force = w.axisUp * F;
                rb.AddForceAtPosition(force, hit.point, ForceMode.Force);
            }
            else
            {
                // Airborne / beyond max travel
                w.grounded = false;
                w.hitPoint = w.anchor.position + w.axisDown * w.rayLen;
                w.groundNormal = w.axisUp; // fallback
                w.prevCompression = w.compression;
                w.currentLen = w.maxLen;
                w.compression = 0f;
            }
        }

        // 2) Anti-roll per axle (simple & robust)
        for (int a = 0; a < axles.Length; a++)
        {
            var axle = axles[a];
            if (!ValidWheelIndex(axle.leftIndex) || !ValidWheelIndex(axle.rightIndex)) continue;

            var WL = wheels[axle.leftIndex];
            var WR = wheels[axle.rightIndex];

            // Positive if left is more compressed than right
            float delta = WL.compression - WR.compression;
            float F = axle.antiRollStiffness * delta;

            // Apply: push down on the more compressed side, up on the less compressed side
            if (WL.grounded)
                rb.AddForceAtPosition(-WL.axisUp * F, WL.hitPoint, ForceMode.Force);
            if (WR.grounded)
                rb.AddForceAtPosition(WR.axisUp * F, WR.hitPoint, ForceMode.Force);
        }

        // 3) Update visuals (optional)
        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (w == null || w.anchor == null || w.visual == null) continue;

            float targetLen = w.currentLen;
            Vector3 targetPos = w.anchor.position + w.axisDown * (targetLen + w.wheelRadius);

            // Smooth movement
            w.visual.position = Vector3.Lerp(w.visual.position, targetPos, 1f - Mathf.Exp(-visualLerp * Time.fixedDeltaTime));

            if (alignVisualToGroundNormal && w.grounded)
            {
                Quaternion toGround = Quaternion.FromToRotation(w.visual.up, w.groundNormal);
                w.visual.rotation = toGround * w.visual.rotation;
            }
        }
    }

    private bool ValidWheelIndex(int i) => i >= 0 && wheels != null && i < wheels.Length;

    /// <summary>
    /// Convenience: choose global k to hit ~target sag ratio (averaged across wheels).
    /// Example: sagRatio=0.3 -> 30% compression at rest.
    /// </summary>
    [ContextMenu("Auto-Tune Springs (30% sag)")]
    public void AutoTuneSprings30()
    {
        AutoTuneSpringsBySag(0.30f);
    }

    public void AutoTuneSpringsBySag(float sagRatio)
    {
        int n = 0;
        float avgSag = 0f;
        foreach (var w in wheels)
        {
            if (w != null && w.anchor != null)
            {
                n++;
                avgSag += Mathf.Clamp01(sagRatio) * Mathf.Max(0.01f, w.travel);
            }
        }
        if (n == 0) return;

        avgSag /= n;

        float W = rb != null ? rb.mass * Physics.gravity.magnitude : 0f; // total weight (N)
        float Ww = W / n; // per-wheel load

        // Choose ONE k so k * avgSag ≈ Ww
        stiffness = Ww / Mathf.Max(0.005f, avgSag);

        // Reasonable default damping (heuristic since unsprung mass not modeled)
        damping = Mathf.Sqrt(Mathf.Max(1f, stiffness)) * 25f;

        preload = 0f; // let spring handle static load
    }

    /// <summary>
    /// One-shot helper to derive rest lengths from transforms for all wheels.
    /// </summary>
    [ContextMenu("Derive All Rest Lengths From 'rest' Transforms")]
    public void DeriveAllRestLengths()
    {
        EnsureRestLengths();
    }

    private void OnDrawGizmosSelected()
    {
        if (wheels == null) return;
        foreach (var w in wheels)
        {
            if (w == null || w.anchor == null) continue;
            Vector3 up = w.anchor.up;
            Vector3 down = -up;

            // Determine current derived values even in edit mode
            float half = Mathf.Max(0f, w.travel) * 0.5f;
            float minLen = Mathf.Max(0f, w.restLength - half);
            float maxLen = w.restLength + half;

            // Centers (wheel centers along axis)
            Vector3 restCenter = w.anchor.position + down * w.restLength;
            Vector3 minCenter = w.anchor.position + down * minLen;
            Vector3 maxCenter = w.anchor.position + down * maxLen;

            // Contact positions (center + radius)
            Vector3 restContact = restCenter + down * w.wheelRadius;
            Vector3 minContact = minCenter + down * w.wheelRadius;
            Vector3 maxContact = maxCenter + down * w.wheelRadius;

            // Rest marker
            if (gizmoShowRest)
            {
                Gizmos.color = gizmoRestColor;
                Gizmos.DrawWireSphere(restCenter, 0.025f);
                Gizmos.DrawLine(w.anchor.position, restCenter);
            }

            // Travel limits (contact line)
            if (gizmoShowTravel)
            {
                Gizmos.color = gizmoTravelColor;
                Gizmos.DrawLine(minContact, maxContact);
                Gizmos.DrawWireSphere(minContact, 0.02f);
                Gizmos.DrawWireSphere(maxContact, 0.02f);
            }

            // Ray visualization (to max)
            if (gizmoShowRay)
            {
                Gizmos.color = gizmoRayColorAir;
                float rayLen = maxLen + w.wheelRadius + 0.05f;
                Gizmos.DrawLine(w.anchor.position, w.anchor.position + down * rayLen);
            }

            // Wheel radius visualization (at rest center)
            if (gizmoShowWheelRadius)
            {
                Gizmos.color = gizmoWheelRadiusColor;
                Gizmos.DrawWireSphere(restCenter, w.wheelRadius);
            }
        }
    }
}