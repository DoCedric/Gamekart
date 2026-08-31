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
        [SerializeField] private float miniTurbo = 1f;
        [SerializeField] private float driftChargeRate = 2f;
        [SerializeField] private float softDriftOptimalAngle = 30f;
        [SerializeField] private float softDriftBonus = 1.5f;

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
        public float MiniTurbo => Mathf.Max(0.1f, miniTurbo);
        public float DriftChargeRate => Mathf.Max(0.1f, driftChargeRate);
        public float SoftDriftOptimalAngle => Mathf.Max(5f, softDriftOptimalAngle);
        public float SoftDriftBonus => Mathf.Max(1f, softDriftBonus);
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
