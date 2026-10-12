using UnityEngine;

/// <summary>
/// Marks a kart as a participant in the race. Added to the kart root at spawn
/// (player and AI alike) and registered with the RaceManager.
/// </summary>
public class Racer : MonoBehaviour
{
    public string DisplayName { get; private set; } = "Racer";
    public bool IsPlayer { get; private set; }

    public void Init(string displayName, bool isPlayer)
    {
        DisplayName = displayName;
        IsPlayer = isPlayer;
    }
}
