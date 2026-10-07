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

        private float boostPeakAmount = 0f;  // speed bonus at the moment the boost started
        private float boostDuration = 0f;    // total duration of the active boost
        private float boostTimer = 0f;       // time remaining on the active boost

        public float CurrentSpeed => currentSpeed;
        public float TargetSpeed => targetSpeed;
        public bool IsBoosting => boostTimer > 0f;

        public void Initialize(KartConfig config, GroundDetector detector)
        {
            kartConfig = config;
            groundDetector = detector;
        }

        /// <summary>
        /// Update movement based on acceleration input and current state.
        /// Called once per FixedUpdate.
        /// </summary>
        public void UpdateMovement(float accelerationInput, int coinCount, float maxSpeedFactor = 1f)
        {
            if (kartConfig == null) return;

            // Calculate max speed considering coins
            float maxSpeed = kartConfig.GetMaxSpeedWithCoins(coinCount) * Mathf.Clamp01(maxSpeedFactor);

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

            currentSpeed += CurrentBoostAmount;
            UpdateBoost(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Start (or refresh) a mini-turbo speed boost. Amount is added on top of normal
        /// speed and decays linearly to zero over the given duration.
        /// </summary>
        public void AddSpeedBoost(float amountAsFractionOfMaxSpeed, float duration)
        {
            if (kartConfig == null || duration <= 0f) return;

            boostPeakAmount = amountAsFractionOfMaxSpeed * kartConfig.MaxSpeed;
            boostDuration = duration;
            boostTimer = duration;
        }

        /// <summary>
        /// Current speed bonus contributed by an active boost (0 when none is active).
        /// </summary>
        public float CurrentBoostAmount => boostTimer > 0f
            ? boostPeakAmount * (boostTimer / boostDuration)
            : 0f;

        private void UpdateBoost(float deltaTime)
        {
            if (boostTimer <= 0f) return;

            boostTimer = Mathf.Max(0f, boostTimer - deltaTime);
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
