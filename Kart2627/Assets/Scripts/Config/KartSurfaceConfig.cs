using UnityEngine;

namespace KartAcademy.Core
{
    /// <summary>
    /// Defines the properties of a surface type (road, grass, ice, etc).
    /// Students can create new surfaces by duplicating this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SurfaceConfig_", menuName = "Kart Academy/Surface Config")]
    public class KartSurfaceConfig : ScriptableObject
    {
        [SerializeField] private string surfaceName = "Road";
        [SerializeField] private float speedModifier = 1f;
        [SerializeField] private float tractionModifier = 1f;
        [SerializeField] private ParticleSystem dustParticlePrefab;
        [SerializeField] private AudioClip surfaceLoopAudio;

        public string SurfaceName => surfaceName;
        public float SpeedModifier => Mathf.Max(0.1f, speedModifier);
        public float TractionModifier => Mathf.Max(0.1f, tractionModifier);
        public ParticleSystem DustParticlePrefab => dustParticlePrefab;
        public AudioClip SurfaceLoopAudio => surfaceLoopAudio;
    }
}
