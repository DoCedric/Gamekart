using UnityEngine;

/// <summary>
/// Switches a kart between "display" (selection screen) and "driving" (track).
/// Display mode turns off physics, input and aiming but leaves visuals,
/// wheels and animators running so an idle animation can still play.
/// Works on any kart prefab; karts without the driving components are left untouched.
/// </summary>
public static class KartDrivingState
{
    public const string KartModelName = "KartModel";

    public static void SetDrivingEnabled(GameObject kart, bool drivingEnabled)
    {
        foreach (KartMotor motor in kart.GetComponentsInChildren<KartMotor>(true))
            motor.enabled = drivingEnabled;
        foreach (KartInputReader input in kart.GetComponentsInChildren<KartInputReader>(true))
            input.enabled = drivingEnabled;
        foreach (KartAimReticle reticle in kart.GetComponentsInChildren<KartAimReticle>(true))
            reticle.enabled = drivingEnabled;

        foreach (Rigidbody body in kart.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = !drivingEnabled;
    }

    /// <summary>The transform the camera should follow, or the kart root if there is no KartModel child.</summary>
    public static Transform FindCameraTarget(GameObject kart)
    {
        foreach (Transform child in kart.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == KartModelName) return child;
        }
        return kart.transform;
    }
}
