using System;
using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Owns the entire drift / mini-turbo state machine: entry, sliding, direction-switch,
    /// charge tiers, release, and the resulting boost. Single source of truth for drift state -
    /// KartController just feeds input in and reads CurrentPhase/CurrentDirection back out.
    /// </summary>
    public class DriftSystem : MonoBehaviour
    {
        public enum DriftPhase { None, DriftStart, Drifting, Boosting }
        public enum DriftDirection { None, Left, Right }

        private const float SteerDeadzone = 0.3f;
        private const float DirectionSwitchThreshold = 0.6f;
        private const float LowSpeedGraceTime = 0.3f;

        private KartConfig kartConfig;
        private MovementSystem movementSystem;
        private SteeringSystem steeringSystem;
        private GroundDetector groundDetector;

        private DriftPhase phase = DriftPhase.None;
        private DriftDirection currentDriftDirection = DriftDirection.None;

        private float driftChargeTime = 0f;
        private int driftChargeTier = 0;
        private Vector3 driftVelocityDirection = Vector3.forward;

        private float airborneTimer = 0f;
        private float lowSpeedTimer = 0f;
        private float directionSwitchCooldownTimer = 0f;
        private float boostPhaseTimer = 0f;

        public event Action<int> OnDriftTierChanged;
        public event Action OnBoostStart;
        public event Action OnBoostEnd;

        public DriftPhase CurrentPhase => phase;
        public DriftDirection CurrentDirection => currentDriftDirection;
        public bool IsDrifting => phase == DriftPhase.DriftStart || phase == DriftPhase.Drifting;
        public int CurrentTier => driftChargeTier;

        /// <summary>Normalized 0-1 progress toward the top mini-turbo tier (for UI meters).</summary>
        public float DriftCharge => kartConfig != null ? Mathf.Clamp01(driftChargeTime / kartConfig.Tier3Time) : 0f;

        public void Initialize(KartConfig config, MovementSystem movement, SteeringSystem steering, GroundDetector detector)
        {
            kartConfig = config;
            movementSystem = movement;
            steeringSystem = steering;
            groundDetector = detector;
        }

        /// <summary>
        /// Advance the drift state machine by one FixedUpdate step.
        /// </summary>
        /// <param name="steerInput">Steering axis, -1..1.</param>
        /// <param name="driftPressed">True on the frame the drift button was first pressed.</param>
        /// <param name="driftHeld">True while the drift button is held.</param>
        /// <param name="grounded">Kart grounded state this frame.</param>
        /// <param name="rb">Kart rigidbody, used for the entry kick impulse.</param>
        /// <param name="deltaTime">Time.fixedDeltaTime.</param>
        public void Tick(float steerInput, bool driftPressed, bool driftHeld, bool grounded, Rigidbody rb, float deltaTime)
        {
            if (kartConfig == null) return;

            switch (phase)
            {
                case DriftPhase.None:
                    if (driftPressed && CanEnterDrift(steerInput, grounded))
                    {
                        EnterDrift(steerInput, rb);
                    }
                    break;

                case DriftPhase.DriftStart:
                    // One-frame transition: the entry kick has already been applied.
                    phase = DriftPhase.Drifting;
                    break;

                case DriftPhase.Drifting:
                    UpdateDrifting(steerInput, driftHeld, grounded, deltaTime);
                    break;

                case DriftPhase.Boosting:
                    UpdateBoosting(steerInput, driftPressed, grounded, rb, deltaTime);
                    break;
            }
        }

        private bool CanEnterDrift(float steerInput, bool grounded)
        {
            if (!grounded) return false;
            if (Mathf.Abs(movementSystem.CurrentSpeed) < kartConfig.MinDriftSpeed) return false;
            if (Mathf.Abs(steerInput) < SteerDeadzone) return false;
            return true;
        }

        private void EnterDrift(float steerInput, Rigidbody rb)
        {
            currentDriftDirection = steerInput > 0f ? DriftDirection.Right : DriftDirection.Left;
            phase = DriftPhase.DriftStart;

            driftChargeTime = 0f;
            driftChargeTier = 0;
            driftVelocityDirection = transform.forward;
            airborneTimer = 0f;
            lowSpeedTimer = 0f;
            directionSwitchCooldownTimer = 0f;

            if (rb != null)
            {
                int sign = currentDriftDirection == DriftDirection.Right ? 1 : -1;
                rb.linearVelocity += Vector3.up * kartConfig.HopForce + transform.right * sign * kartConfig.DriftKickImpulse;
            }
        }

        private void UpdateDrifting(float steerInput, bool driftHeld, bool grounded, float deltaTime)
        {
            if (!driftHeld)
            {
                ReleaseDrift();
                return;
            }

            // Airborne grace: freeze charge but keep sliding until grace runs out.
            if (!grounded)
            {
                airborneTimer += deltaTime;
                if (airborneTimer > kartConfig.AirborneDriftGrace)
                {
                    CancelDrift();
                    return;
                }
            }
            else
            {
                airborneTimer = 0f;
            }

            // Sustained low speed cancels the drift after a short grace window.
            if (Mathf.Abs(movementSystem.CurrentSpeed) < kartConfig.MinDriftSpeed)
            {
                lowSpeedTimer += deltaTime;
                if (lowSpeedTimer > LowSpeedGraceTime)
                {
                    CancelDrift();
                    return;
                }
            }
            else
            {
                lowSpeedTimer = 0f;
            }

            // Velocity direction chases heading over lateralSlipTime seconds - this is what
            // makes the kart slide sideways instead of snapping onto its new heading.
            float t = 1f - Mathf.Exp(-deltaTime / kartConfig.LateralSlipTime);
            driftVelocityDirection = Vector3.Slerp(driftVelocityDirection, transform.forward, t).normalized;

            UpdateDirectionSwitch(steerInput, deltaTime);

            if (grounded)
            {
                driftChargeTime += deltaTime;
                int newTier = TierForChargeTime(driftChargeTime);
                if (newTier != driftChargeTier)
                {
                    driftChargeTier = newTier;
                    OnDriftTierChanged?.Invoke(driftChargeTier);
                }
            }
        }

        private void UpdateDirectionSwitch(float steerInput, float deltaTime)
        {
            directionSwitchCooldownTimer = Mathf.Max(0f, directionSwitchCooldownTimer - deltaTime);

            if (!kartConfig.AllowDriftDirectionSwitch || directionSwitchCooldownTimer > 0f) return;

            bool flickOppositeWhileRight = currentDriftDirection == DriftDirection.Right && steerInput < -DirectionSwitchThreshold;
            bool flickOppositeWhileLeft = currentDriftDirection == DriftDirection.Left && steerInput > DirectionSwitchThreshold;

            if (!flickOppositeWhileRight && !flickOppositeWhileLeft) return;

            currentDriftDirection = currentDriftDirection == DriftDirection.Right ? DriftDirection.Left : DriftDirection.Right;
            directionSwitchCooldownTimer = kartConfig.DirectionSwitchCooldown;
            // Charge time/tier intentionally left untouched - the switch preserves progress.
        }

        private int TierForChargeTime(float time)
        {
            if (time >= kartConfig.Tier3Time) return 3;
            if (time >= kartConfig.Tier2Time) return 2;
            if (time >= kartConfig.Tier1Time) return 1;
            return 0;
        }

        private void ReleaseDrift()
        {
            int tier = driftChargeTier;

            if (tier >= 1)
            {
                (float boostPercent, float duration) = TierBoost(tier);
                movementSystem.AddSpeedBoost(boostPercent, duration);
                boostPhaseTimer = duration;
                phase = DriftPhase.Boosting;
                OnBoostStart?.Invoke();
            }
            else
            {
                phase = DriftPhase.None;
            }

            ResetDriftTracking();
        }

        private (float boostPercent, float duration) TierBoost(int tier)
        {
            switch (tier)
            {
                case 1: return (kartConfig.Tier1BoostPercent, kartConfig.Tier1Duration);
                case 2: return (kartConfig.Tier2BoostPercent, kartConfig.Tier2Duration);
                default: return (kartConfig.Tier3BoostPercent, kartConfig.Tier3Duration);
            }
        }

        private void UpdateBoosting(float steerInput, bool driftPressed, bool grounded, Rigidbody rb, float deltaTime)
        {
            // A new drift can be started immediately, chaining boosts down a long turn.
            // MovementSystem's own boost timer runs independently of our phase, so the
            // residual speed bonus keeps decaying correctly even as we re-enter a drift.
            if (driftPressed && CanEnterDrift(steerInput, grounded))
            {
                EnterDrift(steerInput, rb);
                return;
            }

            boostPhaseTimer -= deltaTime;
            if (boostPhaseTimer <= 0f)
            {
                phase = DriftPhase.None;
                OnBoostEnd?.Invoke();
            }
        }

        private void CancelDrift()
        {
            phase = DriftPhase.None;
            ResetDriftTracking();
        }

        private void ResetDriftTracking()
        {
            currentDriftDirection = DriftDirection.None;
            driftChargeTime = 0f;
            driftChargeTier = 0;
            airborneTimer = 0f;
            lowSpeedTimer = 0f;
        }

        /// <summary>
        /// Get the velocity direction during drift (separate from heading).
        /// This is what causes the slide effect.
        /// </summary>
        public Vector3 GetDriftVelocityDirection()
        {
            return IsDrifting ? driftVelocityDirection : Vector3.zero;
        }

        /// <summary>
        /// Force-cancel any in-progress drift with no boost (e.g. hard collision).
        /// </summary>
        public void ForceCancelDrift()
        {
            phase = DriftPhase.None;
            ResetDriftTracking();
        }
    }
}
