using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Handles airborne physics: gravity, momentum preservation, and air control.
    /// Applies when kart is not grounded.
    /// </summary>
    /// 
    public class AirborneSystem : MonoBehaviour
    {
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float airControlAuthority = 0.3f;  // Reduced steering in air
        [SerializeField] private float airDragFactor = 0.95f;       // Slow speed loss in air

        private float verticalVelocity = 0f;
        private Vector3 airMomentum = Vector3.zero;
        private KartConfig kartConfig;
        private SteeringSystem steeringSystem;

        public float VerticalVelocity => verticalVelocity;
        public Vector3 AirMomentum => airMomentum;

        public void Initialize(KartConfig config, SteeringSystem steering)
        {
            kartConfig = config;
            steeringSystem = steering;
        }

        /// <summary>
        /// Called when kart enters airborne state.
        /// Preserves current forward momentum.
        /// </summary>
        public void EnterAirborne(Vector3 currentVelocity)
        {
            airMomentum = currentVelocity;
            verticalVelocity = currentVelocity.y;
        }

        /// <summary>
        /// Update airborne physics each frame.
        /// </summary>
        public void UpdateAirborne(float deltaTime, float steerInput)
        {
            // Apply gravity
            verticalVelocity += gravity * deltaTime;

            // Limited air steering (weak directional control)
            if (steerInput != 0 && kartConfig != null)
            {
                float airTurnAmount = steerInput * kartConfig.TurnSpeed * airControlAuthority * deltaTime;
                float heading = steeringSystem.CurrentHeading + airTurnAmount;
                steeringSystem.SetHeading(heading);
            }

            // Apply air drag (speed loss while airborne)
            airMomentum *= airDragFactor;
        }

        /// <summary>
        /// Get the velocity to apply while airborne.
        /// Combines forward momentum with gravity.
        /// </summary>
        public Vector3 GetAirborneVelocity(Transform kartTransform)
        {
            Vector3 velocity = kartTransform.forward * airMomentum.magnitude;
            velocity.y = verticalVelocity;
            return velocity;
        }

        /// <summary>
        /// Called when kart lands.
        /// Reset vertical velocity for smooth landing.
        /// </summary>
        public void ExitAirborne()
        {
            verticalVelocity = 0f;
            airMomentum = Vector3.zero;
        }
    }
}
