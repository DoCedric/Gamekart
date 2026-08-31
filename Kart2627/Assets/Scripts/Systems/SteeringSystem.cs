using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Handles steering mechanics with speed-dependent authority.
    /// At low speeds: full steering control.
    /// At high speeds: reduced steering control for realistic feel.
    /// </summary>
    public class SteeringSystem : MonoBehaviour
    {
        private KartConfig kartConfig;
        private MovementSystem movementSystem;
        private float currentHeading = 0f;

        public float CurrentHeading => currentHeading;

        public void Initialize(KartConfig config, MovementSystem movement)
        {
            kartConfig = config;
            movementSystem = movement;
            currentHeading = transform.eulerAngles.y;
        }

        /// <summary>
        /// Apply steering input and update kart rotation.
        /// Should be called once per FixedUpdate.
        /// </summary>
        public void UpdateSteering(float steerInput)
        {
            if (kartConfig == null || movementSystem == null) return;

            // Only steer if moving at meaningful speed
            float speed = Mathf.Abs(movementSystem.CurrentSpeed);
            if (speed < 0.1f)
                return;

            // Calculate steering authority based on speed
            // At max speed, authority is reduced by the curve
            float speedPercent = speed / kartConfig.MaxSpeed;
            float steeringAuthority = kartConfig.SteeringCurve.Evaluate(speedPercent);

            // Calculate turn amount
            float turnAmount = steerInput * kartConfig.TurnSpeed * steeringAuthority;
            currentHeading += turnAmount * Time.fixedDeltaTime;

            // Apply rotation to transform
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentHeading, transform.eulerAngles.z);

        }

        /// <summary>
        /// Manually set heading (useful for state transitions).
        /// </summary>
        public void SetHeading(float heading)
        {
            currentHeading = heading;
        }
    }
}
