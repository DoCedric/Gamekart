using UnityEngine;

/// <summary>
/// Carries the kart chosen on the selection screen over to the track scene.
/// Static on purpose: it only has to survive one scene load.
/// </summary>
public static class KartSelection
{
    public static GameObject SelectedKartPrefab { get; set; }
}
