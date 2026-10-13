using UnityEngine;

/// <summary>
/// Put on an empty in the track scene and add empty child objects where AI karts should
/// start (their position and forward direction are used). One AI kart, picked at random
/// from the roster, is spawned per child and registered with the RaceManager.
/// </summary>
public class AiSpawner : MonoBehaviour
{
    [SerializeField] KartRoster roster;
    [Tooltip("How the AI drives, including difficulty. Create via Create > Kart > AI Driver Settings; a different asset per difficulty works well.")]
    [SerializeField] AiDriverSettings driverSettings;
    [Tooltip("Optional. Found automatically if left empty.")]
    [SerializeField] RaceManager raceManager;

    void Start()
    {
        if (roster == null)
        {
            Debug.LogWarning("AiSpawner: no KartRoster assigned.", this);
            return;
        }

        if (raceManager == null) raceManager = FindFirstObjectByType<RaceManager>();
        if (raceManager == null)
        {
            Debug.LogWarning("AiSpawner: no RaceManager in the scene, AI karts would not be part of the race.", this);
            return;
        }

        if (driverSettings == null)
            Debug.LogWarning("AiSpawner: no AiDriverSettings assigned, using defaults. Create one via Create > Kart > AI Driver Settings.", this);

        int aiNumber = 1;
        foreach (Transform slot in transform)
        {
            GameObject prefab = roster.GetRandomPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("AiSpawner: the roster has no kart prefabs.", this);
                return;
            }

            SpawnAiKart(prefab, slot, $"AI {aiNumber++}");
        }
    }

    void SpawnAiKart(GameObject prefab, Transform slot, string racerName)
    {
        // The motor and aim point look up their input source in Awake, so the kart is
        // built under an inactive parent and only wakes up once its input is swapped.
        var staging = new GameObject("AiKartStaging");
        staging.SetActive(false);

        GameObject kart = Instantiate(prefab, staging.transform);
        kart.transform.SetPositionAndRotation(slot.position, slot.rotation);

        // A respawner already on the prefab (e.g. with its own delay) is kept.
        if (kart.GetComponent<KartRespawner>() == null) kart.AddComponent<KartRespawner>();

        kart.AddComponent<KartAIDriver>().Init(driverSettings);
        foreach (KartInputReader playerInput in kart.GetComponentsInChildren<KartInputReader>(true))
            DestroyImmediate(playerInput); // removed after the AI input is added, which the motor requires
        foreach (KartAimReticle reticle in kart.GetComponentsInChildren<KartAimReticle>(true))
            reticle.Remove();

        Racer racer = kart.AddComponent<Racer>();
        racer.Init(racerName, false);

        kart.transform.SetParent(null, true); // wakes the kart up
        Destroy(staging);

        raceManager.Register(racer);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        foreach (Transform slot in transform)
        {
            Gizmos.DrawWireSphere(slot.position, 0.6f);
            Gizmos.DrawLine(slot.position, slot.position + slot.forward * 2f);
        }
    }
}
