using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Handles acceleration, braking, and coasting mechanics.
    /// Manages the smooth transition of speed based on input and surface conditions.
    /// </summary>
    public class MovementSystem : MonoBehaviour
    {
        private float currentSpeed = 0f;
        private float targetSpeed = 0f;
        private KartConfig kartConfig;
        private GroundDetector groundDetector;

        public float CurrentSpeed => currentSpeed;
        public float TargetSpeed => targetSpeed;

        public void Initialize(KartConfig config, GroundDetector detector)
        {
            kartConfig = config;
            groundDetector = detector;
        }

        /// <summary>
        /// Update movement based on acceleration input and current state.
        /// Called once per FixedUpdate.
        /// </summary>
        public void UpdateMovement(float accelerationInput, int coinCount)
        {
            if (kartConfig == null) return;

            // Calculate max speed considering coins
            float maxSpeed = kartConfig.GetMaxSpeedWithCoins(coinCount);

            // Apply surface speed modifier if grounded
            if (groundDetector.IsGrounded && groundDetector.CurrentSurface != null)
            {
                maxSpeed *= groundDetector.CurrentSurface.SpeedModifier;
            }

            // Calculate target speed
            targetSpeed = accelerationInput * maxSpeed;

            // Smoothly accelerate/decelerate
            float accelRate = accelerationInput > 0 
                ? kartConfig.Acceleration 
                : kartConfig.BrakingStrength;

            // Lerp toward target speed
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * accelRate);

            // Clamp to reasonable range
            currentSpeed = Mathf.Clamp(currentSpeed, -kartConfig.ReverseSpeed, maxSpeed);
        }

        /// <summary>
        /// Get the current velocity vector in world space.
        /// </summary>
        public Vector3 GetVelocity(Transform kartTransform)
        {
            Vector3 velocity = kartTransform.forward * currentSpeed;
            return velocity;
        }

        /// <summary>
        /// Get speed with traction modifier applied.
        /// Useful for physics-based friction calculations.
        /// </summary>
        public float GetEffectiveSpeed()
        {
            if (groundDetector.IsGrounded && groundDetector.CurrentSurface != null)
            {
                return currentSpeed * groundDetector.CurrentSurface.TractionModifier;
            }
            return currentSpeed;
        }
    }
}
