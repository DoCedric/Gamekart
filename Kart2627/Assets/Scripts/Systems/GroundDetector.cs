using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Detects ground contact, surface type, and surface normal.
    /// Uses raycasting to determine if kart is grounded and what surface it's on.
    /// </summary>
    public class GroundDetector : MonoBehaviour
    {
        [SerializeField] private float raycastDistance = 1.5f;
        [SerializeField] private int rayCount = 3;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private bool debugDraw = false;
        [SerializeField] private KartSurfaceConfig defaultSurface;

        private Rigidbody rb;
        private Vector3[] rayPositions;
        private RaycastHit lastHit;
        [SerializeField] private bool isGrounded;
        [SerializeField] private KartSurfaceConfig currentSurface;

        public bool IsGrounded => isGrounded;
        public Vector3 GroundNormal => isGrounded ? lastHit.normal : Vector3.up;
        public KartSurfaceConfig CurrentSurface => currentSurface;
        public RaycastHit LastHit => lastHit;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            InitializeRayPositions();

            if (defaultSurface == null)
            {
                Debug.LogWarning("GroundDetector: No default surface assigned. Surface detection may not work properly.", this);
            }
        }

        private void InitializeRayPositions()
        {
            Bounds bounds = GetComponent<Collider>().bounds;
            rayPositions = new Vector3[rayCount];

            if (rayCount == 1)
            {
                rayPositions[0] = bounds.center;
            }
            else
            {
                float spacing = bounds.extents.x * 1.8f / (rayCount - 1);
                for (int i = 0; i < rayCount; i++)
                {
                    float xOffset = (i - (rayCount - 1) / 2f) * spacing;
                    rayPositions[i] = bounds.center + new Vector3(xOffset, 0, 0);
                }
            }
        }

        private void FixedUpdate()
        {
            DetectGround();
            DetectSurface();
        }

        private void DetectGround()
        {
            isGrounded = false;
            lastHit = new RaycastHit();

            foreach (Vector3 rayPos in rayPositions)
            {
                Vector3 worldRayPos = transform.TransformPoint(rayPos);
                
                if (Physics.Raycast(worldRayPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
                {
                    isGrounded = true;
                    lastHit = hit;

                    if (debugDraw)
                    {
                        Debug.DrawLine(worldRayPos, hit.point, Color.green);
                    }
                    break; // Use first hit
                }
                else if (debugDraw)
                {
                    Debug.DrawLine(worldRayPos, worldRayPos + Vector3.down * raycastDistance, Color.red);
                }
            }
        }

        private void DetectSurface()
        {
            currentSurface = null;

            if (!isGrounded || lastHit.collider == null)
                return;

            // Try to get surface from a SurfaceProvider component on the hit object
            SurfaceProvider provider = lastHit.collider.GetComponent<SurfaceProvider>();
            if (provider != null)
            {
                currentSurface = provider.SurfaceConfig;
            }
            // Fallback to default surface
            else if (defaultSurface != null)
            {
                currentSurface = defaultSurface;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(2f, 0.1f, 2f));
        }
    }
}
