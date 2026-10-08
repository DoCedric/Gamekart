using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Place in the track scene. Pressing the action clears the chosen kart
/// and loads the selection screen again, effectively resetting the game.
/// </summary>
public class ReturnToSelection : MonoBehaviour
{
    [SerializeField] SceneReference selectionScene;
    [SerializeField] InputActionReference returnAction;

    void OnEnable()
    {
        returnAction.action.Enable();
    }

    void OnDisable()
    {
        returnAction.action.Disable();
    }

    void Update()
    {
        if (!returnAction.action.WasPressedThisFrame()) return;

        if (string.IsNullOrEmpty(selectionScene.ScenePath))
        {
            Debug.LogWarning("ReturnToSelection: no selection scene assigned.", this);
            return;
        }

        KartSelection.SelectedKartPrefab = null;
        SceneManager.LoadScene(selectionScene.ScenePath);
    }
}
