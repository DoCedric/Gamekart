using UnityEngine;

/// <summary>
/// Quietly collects live kart data and feeds it to RuntimeDataBus.
/// No interference with existing systems — just reads public properties and updates the bus.
/// </summary>
public class DataCollector : MonoBehaviour
{
    [SerializeField] private KartController kartController;
    [SerializeField] private RuntimeDataBus dataBus;

    private void Start()
    {
        if (kartController == null)
        {
            kartController = GetComponent<KartController>();
        }

        if (kartController == null)
        {
            Debug.LogError("DataCollector: KartController not found!", this);
            enabled = false;
            return;
        }

        if (dataBus == null)
        {
            Debug.LogError("DataCollector: RuntimeDataBus asset not assigned!", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Update data bus with current kart state
        dataBus.UpdateFromKartController(kartController);
    }
}
