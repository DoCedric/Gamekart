using UnityEngine;

/// <summary>
/// Data asset describing how a driving surface changes the kart's handling.
/// All values are multipliers on the KartTuning values; 1 = unchanged.
/// </summary>
[CreateAssetMenu(fileName = "SurfaceType", menuName = "Kart/Surface Type")]
public class SurfaceType : ScriptableObject
{
    [Tooltip("Scales max forward and reverse speed.")]
    public float maxSpeedMultiplier = 1f;

    [Tooltip("Scales forward and reverse acceleration.")]
    public float accelerationMultiplier = 1f;

    [Tooltip("Scales sideways grip. Low values let the kart slide.")]
    public float gripMultiplier = 1f;
}
