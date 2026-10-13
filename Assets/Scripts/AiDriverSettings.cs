using UnityEngine;

/// <summary>
/// How the AI drivers behave. Create one asset per difficulty (Create > Kart > AI Driver Settings)
/// and assign it to the AiSpawner. All AI karts spawned by that spawner share it.
/// </summary>
[CreateAssetMenu(fileName = "AiDriverSettings", menuName = "Kart/AI Driver Settings")]
public class AiDriverSettings : ScriptableObject
{
    [Header("Difficulty (relative to the kart's own tuning)")]
    [Tooltip("Multiplies the kart's top speed. 1.2 = 120%.")]
    [Min(0.1f)] public float topSpeedScale = 1f;
    [Tooltip("Multiplies the kart's acceleration. 1.2 = 120%.")]
    [Min(0.1f)] public float accelerationScale = 1f;

    [Header("Line")]
    [Tooltip("Each AI picks a fixed spot across the gates within this range (0 = always the middle, 1 = up to the road edges).")]
    [Range(0f, 0.9f)] public float maxLateralOffset = 0.5f;
    [Tooltip("At the start each AI drives straight down its own grid lane, with up to this much random drift (in gate-widths from the road centre).")]
    [Range(0f, 0.5f)] public float startLaneDeviation = 0.1f;
    [Tooltip("The line moves from the grid lane to this AI's personal offset over this many gates.")]
    [Min(1)] public int gatesToSettleOnLine = 3;
    [Tooltip("Aim further ahead the faster the kart goes, but never closer than this many metres.")]
    public float minLookAheadMeters = 6f;
    public float lookAheadSeconds = 0.6f;
    [Tooltip("How far toward the following gate the aim point moves as the kart reaches the next one.")]
    [Range(0f, 0.9f)] public float maxLookAheadBlend = 0.5f;

    [Header("Speed (fractions of the scaled top speed)")]
    [Tooltip("Fraction of top speed on straights.")]
    [Range(0.1f, 1f)] public float straightSpeedFraction = 1f;
    [Tooltip("Each AI is randomly this much slower than the limit above, so they don't all lap identically.")]
    [Range(0f, 0.3f)] public float speedVariation = 0.08f;
    [Tooltip("Fraction of top speed in the sharpest corner.")]
    [Range(0.1f, 1f)] public float cornerSpeedFraction = 0.45f;
    [Tooltip("Corner angle (degrees) at which the corner speed fully applies.")]
    public float sharpCornerAngle = 70f;
    [Tooltip("Slowing for a corner starts this many metres before the gate.")]
    public float brakeLookAheadMeters = 30f;
    [Tooltip("How much steering itself reduces the target speed (0 = not at all).")]
    [Range(0f, 0.8f)] public float steerSlowdown = 0.3f;

    [Header("Getting stuck")]
    [Tooltip("Below this speed (m/s) the kart counts as not moving.")]
    public float stuckSpeed = 1f;
    [Tooltip("Respawn after not moving for this many seconds.")]
    public float stuckSeconds = 3f;
    [Tooltip("Ignore being slow for this long after the start, so the launch doesn't count.")]
    public float stuckGraceSeconds = 2f;
}
