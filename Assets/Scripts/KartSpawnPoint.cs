using UnityEngine;

/// <summary>
/// Place on an empty in the track scene. Spawns the kart chosen on the
/// selection screen here, enables driving and points the camera at it.
/// </summary>
public class KartSpawnPoint : MonoBehaviour
{
    [Tooltip("Used when no kart was selected, e.g. when pressing Play directly in the track scene.")]
    [SerializeField] GameObject fallbackKartPrefab;
    [SerializeField] KartCameraFollow cameraFollow;

    void Start()
    {
        GameObject prefab = KartSelection.SelectedKartPrefab != null
            ? KartSelection.SelectedKartPrefab
            : fallbackKartPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("KartSpawnPoint: no selected kart and no fallback prefab assigned.", this);
            return;
        }

        GameObject kart = Instantiate(prefab, transform.position, transform.rotation);
        KartDrivingState.SetDrivingEnabled(kart, true);

        if (cameraFollow != null)
            cameraFollow.target = KartDrivingState.FindCameraTarget(kart);
    }
}
