using UnityEngine;

/// <summary>
/// Anything that can drive a kart: the player's input reader, an AI driver, a replay.
/// KartMotor and KartAimPoint only read these three values.
/// </summary>
public abstract class KartInputSource : MonoBehaviour
{
    public abstract float SteerInput { get; }        // -1 = full left, +1 = full right
    public abstract float AccelerateInput { get; }   // 0 to 1
    public abstract float BrakeReverseInput { get; } // 0 to 1
}
