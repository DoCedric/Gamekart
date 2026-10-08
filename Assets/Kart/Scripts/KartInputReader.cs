using UnityEngine;
using UnityEngine.InputSystem;

public class KartInputReader : MonoBehaviour
{
    [SerializeField] InputActionReference steerAction;
    [SerializeField] InputActionReference accelerateAction;
    [SerializeField] InputActionReference brakeReverseAction;

    public float SteerInput { get; private set; }        // -1 = full left, +1 = full right
    public float AccelerateInput { get; private set; }   // 0 to 1
    public float BrakeReverseInput { get; private set; } // 0 to 1

    void OnEnable()
    {
        steerAction.action.Enable();
        accelerateAction.action.Enable();
        brakeReverseAction.action.Enable();
    }

    void OnDisable()
    {
        steerAction.action.Disable();
        accelerateAction.action.Disable();
        brakeReverseAction.action.Disable();
    }

    void Update()
    {
        SteerInput = Mathf.Clamp(steerAction.action.ReadValue<float>(), -1f, 1f);
        AccelerateInput = Mathf.Clamp01(accelerateAction.action.ReadValue<float>());
        BrakeReverseInput = Mathf.Clamp01(brakeReverseAction.action.ReadValue<float>());
    }
}
