using UnityEngine;
using KartAcademy.Core;

/// <summary>
/// ScriptableObject data bus for sharing live kart data with UI systems.
/// Create an asset instance and reference it in DataCollector and PanelRenderer.
/// Non-intrusive — UI systems just read from this asset.
/// </summary> as

[CreateAssetMenu(fileName = "runtimeDatabus_", menuName = "Kart Academy/Runtime Data Bus")]
public class RuntimeDataBus : ScriptableObject
{
    // Live kart data
    [HideInInspector] public float currentSpeed = 1.0f;
    [HideInInspector] public float targetSpeed;
    [HideInInspector] public float driftIntensity;
    [HideInInspector] public int coinCount;
    [HideInInspector] public bool isAirborne;
    [HideInInspector] public bool isDrifting;

    /// <summary>
    /// Update all live data from KartController
    /// Called by DataCollector each frame
    /// </summary>
    public void UpdateFromKartController(KartController kartController)
    {
        if (kartController == null) return;

        currentSpeed = kartController.CurrentSpeed;
        targetSpeed = kartController.TargetSpeed;
        coinCount = kartController.CoinCount;
        isAirborne = kartController.CurrentState == KartState.Airborne;
        isDrifting = kartController.CurrentState == KartState.DriftingLeft || 
                     kartController.CurrentState == KartState.DriftingRight;

        // Drift intensity from DriftSystem if available
        if (kartController.DriftSystem != null)
        {
            driftIntensity = kartController.DriftSystem.DriftCharge;
        }
    }
}
