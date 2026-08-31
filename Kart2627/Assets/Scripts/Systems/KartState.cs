namespace KartAcademy.Core
{
    /// <summary>
    /// Primary movement states for the kart.
    /// Only one primary state can be active at a time.
    /// </summary>
    public enum KartState
    {
        Grounded,              // On ground, normal driving
        JumpHop,               // Small hop (drift entry)
        DriftingLeft,          // Drifting with left steering
        DriftingRight,         // Drifting with right steering
        Airborne,              // In air (jump/fall)
        Boost,                 // Actively boosting
        Brake,                 // Braking (separate state for clarity)
        Reverse,               // Reverse gear active
        CollisionRecovery      // Post-collision recovery
    }
}
