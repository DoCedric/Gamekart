using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Handles drift mechanics: entry, charge accumulation, soft drift bonuses, and exit.
    /// Critical mechanic: separates heading from velocity direction.
    /// </summary>
    public class DriftSystem : MonoBehaviour
    {
        [SerializeField] private float hopForce = 5f;              // Upward velocity on drift entry
        [SerializeField] private float driftSpeedDecay = 0.98f;    // Kart slows slightly while drifting

        private KartConfig kartConfig;
        private MovementSystem movementSystem;
        private SteeringSystem steeringSystem;
        private GroundDetector groundDetector;

        private float driftCharge = 0f;                            // 0-1 scale
        private float driftDuration = 0f;                          // Time spent drifting
        private Vector3 driftVelocityDirection = Vector3.forward;  // Separate from heading
        private bool isDrifting = false;
        private DriftDirection currentDriftDirection = DriftDirection.None;

        public enum DriftDirection { None, Left, Right }

        public float DriftCharge => driftCharge;
        public bool IsDrifting => isDrifting;
        public DriftDirection CurrentDirection => currentDriftDirection;
        public float DriftDuration => driftDuration;

        public void Initialize(KartConfig config, MovementSystem movement, SteeringSystem steering, GroundDetector detector)
        {
            kartConfig = config;
            movementSystem = movement;
            steeringSystem = steering;
            groundDetector = detector;
        }

        /// <summary>
        /// Attempt to enter drift state. Called when drift input is pressed.
        /// </summary>
        public bool TryEnterDrift(float steerInput, Rigidbody rb)
        {
            if (isDrifting) return false;
            if (!groundDetector.IsGrounded) return false;
            if (Mathf.Abs(movementSystem.CurrentSpeed) < 0.5f) return false;

            // Determine drift direction based on steering input
            if (steerInput > 0.3f)
            {
                currentDriftDirection = DriftDirection.Right;
            }
            else if (steerInput < -0.3f)
            {
                currentDriftDirection = DriftDirection.Left;
            }
            else
            {
                return false; // No clear steering direction
            }

            isDrifting = true;
            driftCharge = 0f;
            driftDuration = 0f;
            driftVelocityDirection = transform.forward;

            // Apply small hop
            if (rb != null)
            {
                rb.linearVelocity += Vector3.up * hopForce;
            }

            return true;
        }

        /// <summary>
        /// Update drift mechanics while drifting.
        /// </summary>
        public void UpdateDrift(float steerInput, float deltaTime)
        {
            if (!isDrifting) return;

            driftDuration += deltaTime;

            // Update velocity direction based on heading
            // This creates the visual "slide" effect
            driftVelocityDirection = transform.forward;

            // Calculate drift charge based on steering angle and duration
            float chargeRate = kartConfig.DriftChargeRate;

            // Soft drift bonus: optimal steering range charges faster
            float steeringAngle = Mathf.Abs(steerInput);
            float optimalAngle = kartConfig.SoftDriftOptimalAngle;

            // Check if within soft drift range (e.g., 30° optimal, ±15° bonus range)
            if (steeringAngle > (optimalAngle - 15f) / 100f && steeringAngle < (optimalAngle + 15f) / 100f)
            {
                chargeRate *= kartConfig.SoftDriftBonus;
            }

            // Accumulate charge
            driftCharge += chargeRate * deltaTime;
            driftCharge = Mathf.Clamp01(driftCharge);

            // Apply slight speed decay while drifting
            // movementSystem will handle actual speed, we just track velocity direction
        }

        /// <summary>
        /// Exit drift and return charge level (0-3 for tier).
        /// </summary>
        public int ExitDrift()
        {
            if (!isDrifting) return 0;

            isDrifting = false;
            driftDuration = 0f;

            // Determine mini-turbo tier (0 = none, 1 = blue, 2 = orange, 3 = purple)
            int tier = 0;
            if (driftCharge > 0.33f) tier = 1;      // Blue
            if (driftCharge > 0.66f) tier = 2;      // Orange
            if (driftCharge > 0.95f) tier = 3;      // Purple

            driftCharge = 0f;
            return tier;
        }

        /// <summary>
        /// Get the velocity direction during drift (separate from heading).
        /// This is what causes the slide effect.
        /// </summary>
        public Vector3 GetDriftVelocityDirection()
        {
            return isDrifting ? driftVelocityDirection : Vector3.zero;
        }

        /// <summary>
        /// Force exit drift (e.g., collision or state change).
        /// </summary>
        public void ForceCancelDrift()
        {
            isDrifting = false;
            driftCharge = 0f;
            driftDuration = 0f;
            currentDriftDirection = DriftDirection.None;
        }
    }
}
