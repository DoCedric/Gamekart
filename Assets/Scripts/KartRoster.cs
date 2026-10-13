using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class kartSelectorData
{
    public GameObject kartPrefab;
    public Sprite kartIcon;
}

/// <summary>
/// The list of karts in the game. Shared by the selection screen and the AI spawner,
/// so adding a kart here makes it available to both.
/// </summary>
[CreateAssetMenu(fileName = "KartRoster", menuName = "Kart/Kart Roster")]
public class KartRoster : ScriptableObject
{
    [SerializeField] List<kartSelectorData> karts = new List<kartSelectorData>();

    public IReadOnlyList<kartSelectorData> Karts => karts;

    /// <summary>A random kart prefab from the roster, or null if the roster has none.</summary>
    public GameObject GetRandomPrefab()
    {
        var candidates = new List<GameObject>();
        foreach (kartSelectorData entry in karts)
        {
            if (entry.kartPrefab != null) candidates.Add(entry.kartPrefab);
        }
        return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
    }
}
