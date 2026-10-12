using UnityEngine;
using UnityEngine.InputSystem;

public class KartInputReader : KartInputSource
{
    [SerializeField] InputActionReference steerAction;
    [SerializeField] InputActionReference accelerateAction;
    [SerializeField] InputActionReference brakeReverseAction;

    float steerInput;
    float accelerateInput;
    float brakeReverseInput;

    public override float SteerInput => steerInput;
    public override float AccelerateInput => accelerateInput;
    public override float BrakeReverseInput => brakeReverseInput;

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
        steerInput = Mathf.Clamp(steerAction.action.ReadValue<float>(), -1f, 1f);
        accelerateInput = Mathf.Clamp01(accelerateAction.action.ReadValue<float>());
        brakeReverseInput = Mathf.Clamp01(brakeReverseAction.action.ReadValue<float>());
    }
}
