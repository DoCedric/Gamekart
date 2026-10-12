using System;
using UnityEngine;

/// <summary>
/// A gate across the track. Place it in driving order under the RaceTrack object,
/// stretch the box over the full width of the road and rotate it so the blue arrow
/// (forward) points in the driving direction. It has no renderer, so players never see it.
/// Reports any rigidbody that enters; the race manager decides what that means.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Log every crossing to the console. Handy while placing checkpoints.")]
    [SerializeField] bool logCrossings;

    [Tooltip("Outline the gate in the Game view while playing: yellow until crossed, green once crossed.")]
    [SerializeField] bool debugGizmo;

    /// <summary>Raised with the rigidbody that entered the gate.</summary>
    public event Action<Checkpoint, Rigidbody> Crossed;

    public Vector3 Center => box.transform.TransformPoint(box.center);

    BoxCollider box;
    LineRenderer debugOutline;
    Material debugMaterial;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (debugGizmo) CreateDebugOutline();
    }

    void OnDestroy()
    {
        if (debugMaterial != null) Destroy(debugMaterial);
    }

    // A real renderer instead of Gizmos, so it shows in the Game view regardless of the Gizmos toggle.
    void CreateDebugOutline()
    {
        Shader shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        debugMaterial = new Material(shader);

        debugOutline = new GameObject("CheckpointDebugOutline").AddComponent<LineRenderer>();
        debugOutline.transform.SetParent(transform, false);
        debugOutline.sharedMaterial = debugMaterial;
        debugOutline.useWorldSpace = true;
        debugOutline.widthMultiplier = 0.15f;
        debugOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        debugOutline.receiveShadows = false;

        Vector3 half = box.size * 0.5f;
        Vector3 CornerWorld(float x, float y, float z) =>
            box.transform.TransformPoint(box.center + new Vector3(x * half.x, y * half.y, z * half.z));

        // One continuous path that traces all 12 edges of the box.
        Vector3 b0 = CornerWorld(-1, -1, -1), b1 = CornerWorld(1, -1, -1), b2 = CornerWorld(1, -1, 1), b3 = CornerWorld(-1, -1, 1);
        Vector3 t0 = CornerWorld(-1, 1, -1), t1 = CornerWorld(1, 1, -1), t2 = CornerWorld(1, 1, 1), t3 = CornerWorld(-1, 1, 1);
        Vector3[] path = { b0, b1, b2, b3, b0, t0, t1, t2, t3, t0, t1, b1, b2, t2, t3, b3 };
        debugOutline.positionCount = path.Length;
        debugOutline.SetPositions(path);

        SetDebugColor(Color.yellow);
    }

    void SetDebugColor(Color color)
    {
        if (debugMaterial == null) return;
        // Shader-specific color property names.
        foreach (string property in new[] { "_UnlitColor", "_BaseColor", "_Color" })
        {
            if (debugMaterial.HasProperty(property)) debugMaterial.SetColor(property, color);
        }
    }

    void Reset()
    {
        BoxCollider newBox = GetComponent<BoxCollider>();
        newBox.isTrigger = true;
        newBox.size = new Vector3(12f, 4f, 1f); // wide across the road, thin along it
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;

        if (logCrossings)
            Debug.Log($"Checkpoint '{name}' crossed by '{body.name}'", this);

        SetDebugColor(Color.green);
        Crossed?.Invoke(this, body);
    }

#if UNITY_EDITOR
    // Edit-mode only; the in-game display is the line renderer above.
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            DrawGate(new Color(0.2f, 0.8f, 1f, 0.15f), new Color(0.2f, 0.8f, 1f, 1f));
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            DrawGate(new Color(1f, 0.9f, 0.2f, 0.3f), new Color(1f, 0.9f, 0.2f, 1f));
    }

    void DrawGate(Color fill, Color line)
    {
        BoxCollider gizmoBox = GetComponent<BoxCollider>();
        if (gizmoBox == null) return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = fill;
        Gizmos.DrawCube(gizmoBox.center, gizmoBox.size);
        Gizmos.color = line;
        Gizmos.DrawWireCube(gizmoBox.center, gizmoBox.size);

        // Direction arrow: driving direction is local +Z.
        Vector3 tail = gizmoBox.center - Vector3.forward * gizmoBox.size.z * 0.5f;
        Vector3 tip = gizmoBox.center + Vector3.forward * Mathf.Max(gizmoBox.size.z, 2f);
        Gizmos.DrawLine(tail, tip);
        Gizmos.DrawLine(tip, tip + new Vector3(-0.5f, 0f, -0.8f));
        Gizmos.DrawLine(tip, tip + new Vector3(0.5f, 0f, -0.8f));
        Gizmos.matrix = previousMatrix;

        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoBox.size.y * 0.5f + 0.5f),
                                  $"{transform.GetSiblingIndex()}: {name}");
    }
#endif
}
