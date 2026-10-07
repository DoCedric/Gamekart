using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Complete kart configuration asset.
    /// Students duplicate and modify this to create new karts without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "KartConfig_", menuName = "Kart Academy/Kart Config")]
    public class KartConfig : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string kartName = "Default Kart";
        [TextArea(2, 4)]
        [SerializeField] private string description = "A default kart configuration";

        [Header("Movement Stats")]
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float brakingStrength = 15f;
        [SerializeField] private float reverseSpeed = 8f;
        [SerializeField] private float weight = 1f;

        [Header("Handling")]
        [SerializeField] private float handling = 5f;
        [SerializeField] private float traction = 1f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private AnimationCurve steeringCurve = AnimationCurve.Linear(0, 1, 20, 0.3f);

        [Header("Drift & Turbo")]
        [SerializeField] private float minDriftSpeed = 6f;
        [SerializeField] private float driftTurnRate = 160f;
        [SerializeField] private float driftMaxSpeedFactor = 0.9f;
        [SerializeField] private float lateralSlipTime = 0.45f;
        [SerializeField] private float driftAngleNudgeRange = 40f;
        [SerializeField] private float driftKickImpulse = 3f;
        [SerializeField] private float hopForce = 5f;

        [Header("Mini-Turbo Tiers")]
        [SerializeField] private float tier1Time = 1.0f;
        [SerializeField] private float tier2Time = 2.0f;
        [SerializeField] private float tier3Time = 3.0f;
        [SerializeField] private float tier1BoostPercent = 0.10f;
        [SerializeField] private float tier2BoostPercent = 0.20f;
        [SerializeField] private float tier3BoostPercent = 0.30f;
        [SerializeField] private float tier1Duration = 0.5f;
        [SerializeField] private float tier2Duration = 0.8f;
        [SerializeField] private float tier3Duration = 1.2f;

        [Header("Drift Edge Cases")]
        [SerializeField] private float airborneDriftGrace = 0.4f;
        [SerializeField] private bool allowDriftDirectionSwitch = true;
        [SerializeField] private float directionSwitchCooldown = 0.18f;

        [Header("Coin Scaling")]
        [SerializeField] private float coinSpeedBonus = 0.5f;

        [Header("Camera")]
        [SerializeField] private float cameraFollowDistance = 8f;
        [SerializeField] private float cameraLookAheadDistance = 5f;
        [SerializeField] private float cameraHeight = 3f;
        [SerializeField] private float cameraDriftOffset = 2f;

        [Header("Visual & Audio References")]
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private AudioClip engineLoopClip;
        [SerializeField] private AudioClip boostAudioClip;
        [SerializeField] private AudioClip driftStartClip;
        [SerializeField] private ParticleSystem driftSparksPrefab;
        [SerializeField] private ParticleSystem boostTrailPrefab;

        // Properties for read-only access
        public string KartName => kartName;
        public string Description => description;
        public float MaxSpeed => Mathf.Max(1f, maxSpeed);
        public float Acceleration => Mathf.Max(0.1f, acceleration);
        public float BrakingStrength => Mathf.Max(0.1f, brakingStrength);
        public float ReverseSpeed => Mathf.Max(0.1f, reverseSpeed);
        public float Weight => Mathf.Max(0.1f, weight);
        public float Handling => Mathf.Max(0.1f, handling);
        public float Traction => Mathf.Max(0.1f, traction);
        public float TurnSpeed => Mathf.Max(0.1f, turnSpeed);
        public AnimationCurve SteeringCurve => steeringCurve ?? AnimationCurve.Linear(0, 1, 20, 0.3f);
        public float MinDriftSpeed => Mathf.Max(0f, minDriftSpeed);
        public float DriftTurnRate => Mathf.Max(1f, driftTurnRate);
        public float DriftMaxSpeedFactor => Mathf.Clamp01(driftMaxSpeedFactor);
        public float LateralSlipTime => Mathf.Max(0.01f, lateralSlipTime);
        public float DriftAngleNudgeRange => Mathf.Max(0f, driftAngleNudgeRange);
        public float DriftKickImpulse => Mathf.Max(0f, driftKickImpulse);
        public float HopForce => Mathf.Max(0f, hopForce);
        public float Tier1Time => Mathf.Max(0.01f, tier1Time);
        public float Tier2Time => Mathf.Max(Tier1Time, tier2Time);
        public float Tier3Time => Mathf.Max(Tier2Time, tier3Time);
        public float Tier1BoostPercent => Mathf.Max(0f, tier1BoostPercent);
        public float Tier2BoostPercent => Mathf.Max(0f, tier2BoostPercent);
        public float Tier3BoostPercent => Mathf.Max(0f, tier3BoostPercent);
        public float Tier1Duration => Mathf.Max(0f, tier1Duration);
        public float Tier2Duration => Mathf.Max(0f, tier2Duration);
        public float Tier3Duration => Mathf.Max(0f, tier3Duration);
        public float AirborneDriftGrace => Mathf.Max(0f, airborneDriftGrace);
        public bool AllowDriftDirectionSwitch => allowDriftDirectionSwitch;
        public float DirectionSwitchCooldown => Mathf.Max(0f, directionSwitchCooldown);
        public float CoinSpeedBonus => Mathf.Max(0f, coinSpeedBonus);
        public float CameraFollowDistance => Mathf.Max(1f, cameraFollowDistance);
        public float CameraLookAheadDistance => Mathf.Max(0f, cameraLookAheadDistance);
        public float CameraHeight => cameraHeight;
        public float CameraDriftOffset => cameraDriftOffset;
        public GameObject VisualPrefab => visualPrefab;
        public AudioClip EngineLoopClip => engineLoopClip;
        public AudioClip BoostAudioClip => boostAudioClip;
        public AudioClip DriftStartClip => driftStartClip;
        public ParticleSystem DriftSparksPrefab => driftSparksPrefab;
        public ParticleSystem BoostTrailPrefab => boostTrailPrefab;

        /// <summary>
        /// Calculate max speed considering coins (0-10).
        /// </summary>
        public float GetMaxSpeedWithCoins(int coinCount)
        {
            int clampedCoins = Mathf.Clamp(coinCount, 0, 10);
            return MaxSpeed + (clampedCoins * CoinSpeedBonus);
        }
    }
}
