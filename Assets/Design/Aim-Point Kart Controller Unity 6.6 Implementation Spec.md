# Aim-Point Kart Controller: Unity 6.6 Implementation Spec

Oct 7, 2026 · @Cedric

## Decision

Build the kart as a rolling-sphere arcade controller steered by an **aim point**. The steer input swings an aim ray left or right of the kart's heading, the ray hits the track, and the kart drives the circular arc that reaches that point. Accelerate, brake and reverse are separate inputs. This is the only steering method in this spec: no free camera aiming, no mouse, no wheel simulation.

The steering rule is pure pursuit, where the player supplies the target. For an aim point at angle α from the heading and distance l\_d ahead, the kart follows an arc with this curvature, and its turn rate is speed times curvature:

```latex
\kappa = \frac{2 \sin(\alpha)}{l_d} \qquad \text{turnRate} = v \cdot \kappa
```

The curvature formula is the standard pure pursuit law, from [this explainer](https://av2.readthedocs.io/en/latest/theory/pure-pursuit.html). The stick sets α. The aim ray's pitch and height set l\_d, because a ray from height h pitched down by θ lands about h / tan(θ) ahead on level ground. A short l\_d at low speed gives tight, Mario Kart-like turns; a long l\_d at speed gives wide, stable arcs.

**Why this method fits the brief**

- It needs one steer axis, so a gamepad stick, a joystick or the left and right arrow keys all work.
- The ray lands on the real track surface, so the target follows slopes and banking instead of floating in the air.
- Turn radius changes with speed through two tuning values (aim yaw and ray pitch), not through a vehicle simulation.
- A rolling sphere with the visual kart on top is a common arcade-kart approach; a [forum thread on Mario Kart-style physics](https://gamedev.net/forums/topic/699625-arcade-car-physics/5394776/) points the same way.

**One honest consequence.** The aim is relative to the kart, so a held stick gives a steady arc, much like analog steering whose radius is shaped by the lookahead geometry. The first prototype must prove this feels better than plain steering-angle control. If it does not, the fallback is in Rejected options.

## What jam games show

Games where the player points and the car handles the steering work well when the car reacts quickly and the player can see the target, and the comments point to five changes to this spec. The evidence is thin. I read game pages and public comments but did not play the games; most are top-down 2D with few ratings, and none uses a 3D chase camera with a raycast aim point.

| Game | How you drive | What players reported | Lesson for this spec |
| --- | --- | --- | --- |
| [Cursor Drifter](https://soffu.itch.io/cursor-drifter) (Godot, 4.6 of 5 from 28 ratings) | The car always follows the mouse cursor and drifts. | One player called the car controls excellent; others praised the responsiveness and satisfying drift. One player needed a while to learn that no click is needed. | Closest match to our idea, and it is well liked. Drift feel matters; tell players how the control works. |
| [A mouse-steered jam racer](https://itch.io/post/10423771) (name not shown in the comment) | Mouse movement steers the car. | Tight turns felt awkward because one mouse stroke could not turn far enough; the reviewer asked for a sensitivity slider, and noted that slowing down to turn was always possible. | Full lock must reach tight turns; add a sensitivity setting; do not make slowing down the safe way to turn. |
| [Raceblades](https://k-embee.itch.io/raceblades) (Juniper jam) | Mouse steers the car; buttons run the engine and brake. | Players found the car sections fun once they got the hang of them; the developer admitted never telling players that all movement uses the cursor. | A learning curve exists even for simple pointing; teach it in the first seconds. |
| [Twist Drive](https://itch.io/post/8196192/view-in-topic) (jam) | The mouse rotates the world; the car cannot be steered. | Players liked it, but camera shake or spin could fling the car off the track, and the car often spun out after landing; one asked for it to stay more glued to the track. | Keep the camera calm and the kart stuck to the ground, which `extraDownwardAcceleration` is for. |
| [A one-button jam racer](https://itch.io/post/13716817) | One button, apparently turning in a single direction. | The reviewer could not find a steady way round corners and had no way to correct a turn that went too far. | Steering must be proportional and work in both directions. |
| [A car-and-weapons jam game](https://itch.io/post/16042207) | Not stated; the developer left mouse camera look out on purpose. | A reviewer wished to see more road ahead and kept moving the mouse pointer up expecting the camera to move. | A pointer reads as where I look, so the camera must show the road ahead. |

One more data point is a developer claim, not player feedback: the maker of [a jam game](https://vladimivrdbvy.itch.io/dont-run/comments) said mouse steering with W and S for speed was far more fun than steering with A and D, which he called too hard. Our arrow keys are digital, so they need smoothing to feel analog.

**Changes this makes to the spec**

1. **Show the aim point.** `KartAimReticle` draws it on the track. Cursor games always show the target; ours must too.
2. **Shape the stick.** `steerResponseExponent` (1.5) and `steerSensitivity` (1.0) give fine control near centre and a player-facing setting, from the sensitivity feedback.
3. **Make arrow keys feel analog.** Keep `aimYawResponsePerSecond` around 10 so a key press eases the aim across instead of jumping.
4. **Frame the road ahead.** The camera must keep the reticle on screen with road beyond it and must never shake from collisions.
5. **Teach it on screen.** Show the reticle from the first frame and a one-line hint such as "Move the aim to steer".

**Kept as is.** Turning never slows the kart, so there is no slow-down-to-turn shortcut, and steering is proportional both ways.

**Open question for playtests.** In the cursor games the target stays where you put it. Here the aim re-centres when you release the stick, so the kart stops turning at once. A world-anchored aim that stays put would feel more like those games but straightens the kart more slowly. I did not adopt it; try it only if playtesters find the re-centring abrupt.

## Player-facing behaviour

The kart does exactly this for each input. The code in the Code section implements every row.

| Input | What the kart does | Edge cases |
| --- | --- | --- |
| Steer left or right | Aim yaw moves toward steer × max aim yaw (smaller at speed). The kart arcs toward the aim point. | Stick centred means straight ahead. A stationary kart does not turn, because turn rate is speed × curvature. |
| Accelerate | Forward acceleration, weaker near top speed (a curve). | While rolling backward, accelerate brakes first, then goes forward. |
| Brake | Strong deceleration while moving forward. | Never goes below zero speed by itself while moving forward. |
| Reverse (hold brake at a standstill) | Once forward speed is below `reverseEngageSpeed`, holding brake reverses up to `maxReverseSpeed`. | Steering works naturally in reverse: steer right and the nose swings left, as in a real car. |
| Accelerate and brake together | Brakes to a halt and never reverses. | Reserved for a later drift or hop mechanic. |
| No input | Coasts to a stop at `coastDeceleration`. | The kart keeps its heading. |
| Airborne | No steering or throttle; the kart keeps its velocity and extra gravity pulls it down. | Landing re-aligns the model to the ground normal. |
| Aim ray misses the track | Uses a fallback point `missFallbackDistance` ahead at the same aim yaw. | The debug gizmo turns red; the kart never gets a NaN or a zero target. |

## Unity 6.6 setup

Target Unity 6.6 (6000.6) with the Input System package 1.19.0, which [Unity's package page](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html) lists as the version released for 6000.6. When I searched on 7 Oct 2026, the 6.6 documentation was still labelled Alpha, so pin the exact editor version and re-check any API that fails to compile.

Physics names changed in Unity 6: `Rigidbody.velocity` is now `linearVelocity`, and drag is now `linearDamping` ([source](https://uhiyama-lab.com/en/notes/unity/unity-rigidbody-guide/)). All code here uses the new names. Do not use the legacy `UnityEngine.Input` class anywhere.

**Project setup, in order**

1. In Player Settings, set Active Input Handling to Input System Package (New), then restart the editor.
2. Add two layers: `Track` and `Kart`. Every drivable surface goes on `Track`; every kart object goes on `Kart`.
3. Set Fixed Timestep to 0.01667 (60 Hz) under Project Settings, Time.
4. Create a physics material named `KartSphereSlippery` with dynamic friction 0, static friction 0 and friction combine Minimum. In Unity 6 this asset type may be named `PhysicsMaterial`; use whichever class your editor offers.
5. Create the `KartTuning` asset (code below) and the input actions asset (next section).
6. Build the kart hierarchy below.

**Kart hierarchy**

| Object | Components and settings | Notes |
| --- | --- | --- |
| `Kart` (empty root, layer `Kart`) | `KartInputReader`, `KartAimPoint`, `KartMotor` | Never move or rotate this object. Only the two children below move. |
| `Kart/SphereBody` | `Rigidbody`: mass 1, linear damping 0, angular damping 0.05, interpolation Interpolate, collision detection Continuous, freeze rotation X, Y and Z. `SphereCollider`: radius 0.5, material `KartSphereSlippery`. | The sphere slides like a puck; heading is stored in `KartModel`. Continuous detection stops tunnelling at 24 m/s. |
| `Kart/KartModel` | The visual mesh. No collider. | The script moves it to the sphere in `LateUpdate` and rotates it toward the heading and ground normal. |

The aim ray does not read the scene camera. Any chase camera, whether a simple follow script or Cinemachine, is independent of the controller; give it rotation smoothing so it does not fight the kart's turn. Keep it high and far enough back that the aim reticle is always on screen with road beyond it, and never let collisions rotate or shake it.

## Input actions

Create one asset, `KartControls.inputactions`, with one action map named `Kart`. Every action is a single float axis, so the same three actions cover gamepad, joystick and arrow keys. The code reads each action through an `InputActionReference` and calls `ReadValue<float>()`, the pattern shown in the [Input System API docs](https://docs.unity3d.com/Packages/com.unity.inputsystem%401.12/api/UnityEngine.InputSystem.InputAction.CallbackContext.html). Do not use a `PlayerInput` component or UnityEvents.

| Action | Type | Bindings | Processor |
| --- | --- | --- | --- |
| `Steer` | Value, Axis | `<Gamepad>/leftStick/x`; `<Joystick>/stick/x`; 1D Axis composite with Negative `<Keyboard>/leftArrow` and Positive `<Keyboard>/rightArrow` | `AxisDeadzone` min 0.15, max 1 |
| `Accelerate` | Value, Axis | `<Gamepad>/rightTrigger`; `<Gamepad>/buttonSouth`; `<Joystick>/trigger`; `<Keyboard>/upArrow` | none |
| `BrakeReverse` | Value, Axis | `<Gamepad>/leftTrigger`; `<Gamepad>/buttonWest`; `<Keyboard>/downArrow` | none |

The 1D Axis composite takes a Positive and a Negative part, as in [Unity's bindings manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@0.2/manual/ActionBindings.html), which is how two arrow keys become one -1 to 1 axis. Add a control scheme for each device family only if you need device-specific rebinding. The actions need to be enabled; `KartInputReader` enables them in `OnEnable` and disables them in `OnDisable`.

The `Joystick` bindings are generic and device layouts vary, so test them with the target hardware. Add `<Keyboard>/a`, `<Keyboard>/d`, `<Keyboard>/w` and `<Keyboard>/s` as extra bindings if WASD should also work.

## Tuning variables

All tuning lives in one `KartTuning` ScriptableObject, so designers can change feel without touching code. The start values are my own first guesses for a kart about 1 m wide, not figures from a source; tune them in play. Names below match the code exactly.

| Variable | Unit | Start | Effect |
| --- | --- | --- | --- |
| `maxForwardSpeed` | m/s | 24 | Top speed. |
| `maxReverseSpeed` | m/s | 8 | Top speed backward. |
| `reverseEngageSpeed` | m/s | 0.5 | Below this forward speed, holding brake starts reversing. |
| `forwardAcceleration` | m/s² | 14 | Base acceleration at full throttle. |
| `accelerationMultiplierBySpeedFraction` | curve | 1 to 0.2 | Scales acceleration from standstill (left) to top speed (right). |
| `reverseAcceleration` | m/s² | 8 | How fast the kart picks up speed backward. |
| `brakeDeceleration` | m/s² | 30 | Braking strength; also used when throttle is pressed while rolling backward. |
| `coastDeceleration` | m/s² | 4 | Slowdown with no input. |
| `aimRayOriginHeight` | m | 2.2 | Height above the sphere where the aim ray starts. With the pitch, sets lookahead distance. |
| `maxAimYawDegreesAtLowSpeed` | degrees | 40 | Aim angle at full stick when slow. Larger means tighter turns. |
| `maxAimYawDegreesAtTopSpeed` | degrees | 18 | Aim angle at full stick at top speed. Smaller means calmer high-speed steering. |
| `aimRayPitchDownDegreesAtLowSpeed` | degrees | 35 | Ray pitch when slow. Steeper means a nearer aim point. |
| `aimRayPitchDownDegreesAtTopSpeed` | degrees | 12 | Ray pitch at top speed. Shallower means a farther aim point. |
| `aimYawResponsePerSecond` | 1/s | 10 | How quickly aim yaw follows the stick. Higher is snappier. |
| steerResponseExponent | none | 1.5 | Shapes the steer input. 1 is linear; higher gives finer control near centre while full lock stays reachable. |
| steerSensitivity | multiplier | 1.0 | Player-facing setting that scales the shaped steer input, clamped to 1. Offer it in the options menu. |
| `maxAimRayDistance` | m | 60 | Ray length limit. |
| `missFallbackDistance` | m | 8 | Distance of the fallback aim point when the ray hits nothing. |
| `minLookaheadDistance` | m | 1.5 | Floor for l\_d, so curvature cannot blow up when the hit is very close. |
| `maxTurnRateDegreesPerSecond` | degrees/s | 130 | Hard cap on rotation speed. |
| `sidewaysGripPerSecond` | 1/s | 8 | How fast sideways velocity dies. Lower slides more. |
| `trackLayers` | layer mask | `Track` only | Layers the aim ray and ground check can hit. Must exclude `Kart`. |
| `groundCheckExtraDistance` | m | 0.3 | Ground check reach beyond the sphere radius. |
| `extraDownwardAcceleration` | m/s² | 15 | Extra gravity that keeps the sphere on the track over crests. |
| `modelAlignToGroundPerSecond` | 1/s | 12 | How quickly the model tilts to the ground normal. |

**Worked example on level ground**, to sanity-check the start values. At low speed the ray lands about 2.2 / tan(35°) = 3.1 m ahead; at full stick (40°) the curvature is 2 × sin(40°) / 3.1 = 0.41 per metre, a turn radius of about 2.4 m. At top speed the ray lands about 2.2 / tan(12°) = 10.3 m ahead; at full stick (18°) the curvature is 0.060 per metre, a radius of about 17 m and a turn rate of about 82 degrees per second at 24 m/s.

## Code

Five files, each in its own `.cs` file with the same name as the class. `KartTuning` holds numbers, `KartInputReader` reads input, `KartAimPoint` produces the aim point, and `KartMotor` turns and drives the sphere. I wrote this from the sources above and my own knowledge of the Unity API; I have not compiled it, so the first task for the coding agent is to compile it and fix any API-name drift in 6.6.

### KartTuning.cs

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "KartTuning", menuName = "Kart/Kart Tuning")]
public class KartTuning : ScriptableObject
{
    [Header("Speed (meters per second)")]
    public float maxForwardSpeed = 24f;
    public float maxReverseSpeed = 8f;
    public float reverseEngageSpeed = 0.5f;

    [Header("Acceleration (meters per second squared)")]
    public float forwardAcceleration = 14f;
    public AnimationCurve accelerationMultiplierBySpeedFraction = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
    public float reverseAcceleration = 8f;
    public float brakeDeceleration = 30f;
    public float coastDeceleration = 4f;

    [Header("Aim ray")]
    public float aimRayOriginHeight = 2.2f;
    public float maxAimYawDegreesAtLowSpeed = 40f;
    public float maxAimYawDegreesAtTopSpeed = 18f;
    public float aimRayPitchDownDegreesAtLowSpeed = 35f;
    public float aimRayPitchDownDegreesAtTopSpeed = 12f;
    public float aimYawResponsePerSecond = 10f;
    public float steerResponseExponent = 1.5f;
    public float steerSensitivity = 1f;
    public float maxAimRayDistance = 60f;
    public float missFallbackDistance = 8f;
    public float minLookaheadDistance = 1.5f;

    [Header("Turning and grip")]
    public float maxTurnRateDegreesPerSecond = 130f;
    public float sidewaysGripPerSecond = 8f;

    [Header("Ground")]
    public LayerMask trackLayers;
    public float groundCheckExtraDistance = 0.3f;
    public float extraDownwardAcceleration = 15f;
    public float modelAlignToGroundPerSecond = 12f;
}
```

### KartInputReader.cs

```csharp
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
```

### KartAimPoint.cs

```csharp
using UnityEngine;

[RequireComponent(typeof(KartInputReader))]
public class KartAimPoint : MonoBehaviour
{
    [SerializeField] KartTuning tuning;
    [SerializeField] Rigidbody sphereBody;
    [SerializeField] Transform kartModel;

    public Vector3 AimPointWorld { get; private set; }
    public bool AimRayHitTrack { get; private set; }
    public float AimYawDegrees { get; private set; }

    KartInputReader inputReader;

    void Awake() => inputReader = GetComponent<KartInputReader>();

    // Called by KartMotor once per FixedUpdate, before steering.
    public void UpdateAim(float speedFraction, float deltaTime)
    {
        float maxAimYaw = Mathf.Lerp(tuning.maxAimYawDegreesAtLowSpeed, tuning.maxAimYawDegreesAtTopSpeed, speedFraction);
        float rawSteer = inputReader.SteerInput;
        float shapedSteer = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), tuning.steerResponseExponent);
        float steer = Mathf.Clamp(shapedSteer * tuning.steerSensitivity, -1f, 1f);
        float targetAimYaw = steer * maxAimYaw;
        float smoothing = 1f - Mathf.Exp(-tuning.aimYawResponsePerSecond * deltaTime);
        AimYawDegrees = Mathf.Lerp(AimYawDegrees, targetAimYaw, smoothing);

        float pitchDownDegrees = Mathf.Lerp(tuning.aimRayPitchDownDegreesAtLowSpeed, tuning.aimRayPitchDownDegreesAtTopSpeed, speedFraction);

        Vector3 up = kartModel.up;
        Vector3 right = kartModel.right;
        Vector3 forward = kartModel.forward;
        Quaternion yawRotation = Quaternion.AngleAxis(AimYawDegrees, up);

        Vector3 rayOrigin = sphereBody.position + up * tuning.aimRayOriginHeight;
        Vector3 rayDirection = yawRotation * (Quaternion.AngleAxis(pitchDownDegrees, right) * forward);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, tuning.maxAimRayDistance,
                            tuning.trackLayers, QueryTriggerInteraction.Ignore))
        {
            AimPointWorld = hit.point;
            AimRayHitTrack = true;
        }
        else
        {
            AimPointWorld = sphereBody.position + yawRotation * forward * tuning.missFallbackDistance;
            AimRayHitTrack = false;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || sphereBody == null) return;
        Gizmos.color = AimRayHitTrack ? Color.green : Color.red;
        Gizmos.DrawLine(sphereBody.position, AimPointWorld);
        Gizmos.DrawSphere(AimPointWorld, 0.3f);
    }
}
```

### KartMotor.cs

```csharp
using UnityEngine;

[RequireComponent(typeof(KartInputReader), typeof(KartAimPoint))]
public class KartMotor : MonoBehaviour
{
    [SerializeField] KartTuning tuning;
    [SerializeField] Rigidbody sphereBody;
    [SerializeField] Transform kartModel;
    [SerializeField] float sphereRadius = 0.5f;
    [SerializeField] float modelHeightBelowSphereCenter = 0.5f;

    public float ForwardSpeed { get; private set; } // signed meters per second along the heading

    KartInputReader inputReader;
    KartAimPoint aimPoint;
    bool isGrounded;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        inputReader = GetComponent<KartInputReader>();
        aimPoint = GetComponent<KartAimPoint>();
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        float speedFraction = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / tuning.maxForwardSpeed);

        CheckGround();
        aimPoint.UpdateAim(speedFraction, deltaTime);
        AlignModelToGround(deltaTime);

        if (isGrounded)
        {
            TurnTowardAimPoint(deltaTime);
            DriveAlongHeading(deltaTime);
            sphereBody.AddForce(-groundNormal * tuning.extraDownwardAcceleration, ForceMode.Acceleration);
        }
        else
        {
            sphereBody.AddForce(Vector3.down * tuning.extraDownwardAcceleration, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        kartModel.position = sphereBody.transform.position - kartModel.up * modelHeightBelowSphereCenter;
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(sphereBody.position, Vector3.down, out RaycastHit hit,
                                     sphereRadius + tuning.groundCheckExtraDistance,
                                     tuning.trackLayers, QueryTriggerInteraction.Ignore);
        groundNormal = isGrounded ? hit.normal : Vector3.up;
    }

    void AlignModelToGround(float deltaTime)
    {
        Quaternion alignedRotation = Quaternion.FromToRotation(kartModel.up, groundNormal) * kartModel.rotation;
        float smoothing = 1f - Mathf.Exp(-tuning.modelAlignToGroundPerSecond * deltaTime);
        kartModel.rotation = Quaternion.Slerp(kartModel.rotation, alignedRotation, smoothing);
    }

    // Pure pursuit: curvature = 2 * sin(angleToAim) / lookaheadDistance; turnRate = speed * curvature.
    void TurnTowardAimPoint(float deltaTime)
    {
        Vector3 heading = Vector3.ProjectOnPlane(kartModel.forward, groundNormal).normalized;
        Vector3 toAimPoint = Vector3.ProjectOnPlane(aimPoint.AimPointWorld - sphereBody.position, groundNormal);

        float lookaheadDistance = Mathf.Max(toAimPoint.magnitude, tuning.minLookaheadDistance);
        float angleToAimDegrees = Vector3.SignedAngle(heading, toAimPoint, groundNormal);
        float curvaturePerMeter = 2f * Mathf.Sin(angleToAimDegrees * Mathf.Deg2Rad) / lookaheadDistance;

        float turnRateDegreesPerSecond = ForwardSpeed * curvaturePerMeter * Mathf.Rad2Deg;
        turnRateDegreesPerSecond = Mathf.Clamp(turnRateDegreesPerSecond,
            -tuning.maxTurnRateDegreesPerSecond, tuning.maxTurnRateDegreesPerSecond);

        kartModel.rotation = Quaternion.AngleAxis(turnRateDegreesPerSecond * deltaTime, groundNormal) * kartModel.rotation;
    }

    void DriveAlongHeading(float deltaTime)
    {
        Vector3 heading = Vector3.ProjectOnPlane(kartModel.forward, groundNormal).normalized;
        Vector3 rightOfHeading = Vector3.Cross(groundNormal, heading);

        Vector3 velocity = sphereBody.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, heading);
        float sidewaysSpeed = Vector3.Dot(velocity, rightOfHeading);
        Vector3 remainingVelocity = velocity - heading * forwardSpeed - rightOfHeading * sidewaysSpeed;

        forwardSpeed = ApplyThrottleAndBrake(forwardSpeed, deltaTime);
        float gripSmoothing = 1f - Mathf.Exp(-tuning.sidewaysGripPerSecond * deltaTime);
        sidewaysSpeed = Mathf.Lerp(sidewaysSpeed, 0f, gripSmoothing);

        sphereBody.linearVelocity = heading * forwardSpeed + rightOfHeading * sidewaysSpeed + remainingVelocity;
        ForwardSpeed = forwardSpeed;
    }

    float ApplyThrottleAndBrake(float speed, float deltaTime)
    {
        float accelerate = inputReader.AccelerateInput;
        float brakeReverse = inputReader.BrakeReverseInput;
        bool accelerateHeld = accelerate > 0.05f;
        bool brakeHeld = brakeReverse > 0.05f;

        if (accelerateHeld && !brakeHeld)
        {
            if (speed < 0f)
            {
                speed += tuning.brakeDeceleration * deltaTime; // rolling backward: brake first
            }
            else
            {
                float speedFraction = Mathf.Clamp01(speed / tuning.maxForwardSpeed);
                float curve = tuning.accelerationMultiplierBySpeedFraction.Evaluate(speedFraction);
                speed += tuning.forwardAcceleration * curve * accelerate * deltaTime;
            }
        }
        else if (brakeHeld && !accelerateHeld && speed <= tuning.reverseEngageSpeed)
        {
            speed -= tuning.reverseAcceleration * brakeReverse * deltaTime; // reverse
        }
        else if (brakeHeld)
        {
            speed = Mathf.MoveTowards(speed, 0f, tuning.brakeDeceleration * brakeReverse * deltaTime); // brake, never reverse
        }
        else
        {
            speed = Mathf.MoveTowards(speed, 0f, tuning.coastDeceleration * deltaTime);
        }

        return Mathf.Clamp(speed, -tuning.maxReverseSpeed, tuning.maxForwardSpeed);
    }
}
```

### KartAimReticle.cs

Shows the aim point on the track so the player sees where the kart is heading. This is required, not optional: see What jam games show. Put it on the `Kart` root, assign `KartAimPoint`, a flat reticle object with no collider, and its renderer. Use an unlit or sprite material.

```csharp
using UnityEngine;

public class KartAimReticle : MonoBehaviour
{
    [SerializeField] KartAimPoint aimPoint;
    [SerializeField] Transform reticle;
    [SerializeField] Renderer reticleRenderer;
    [SerializeField] Color hitColor = Color.white;
    [SerializeField] Color missColor = Color.red;
    [SerializeField] float heightAboveTrack = 0.05f;

    void LateUpdate()
    {
        reticle.position = aimPoint.AimPointWorld + Vector3.up * heightAboveTrack;
        reticleRenderer.material.color = aimPoint.AimRayHitTrack ? hitColor : missColor;
    }
}
```

**Wiring.** Put the three components on the `Kart` root. Assign `KartTuning`, `SphereBody` and `KartModel` on `KartAimPoint` and `KartMotor`, and the three `InputActionReference` actions on `KartInputReader`. Set `trackLayers` in the `KartTuning` asset to `Track` only.

## Per-frame flow

Input is read in `KartInputReader.Update`. Everything else runs in `KartMotor.FixedUpdate`, in the order below.

&#91;embedded content: control pipeline · 8 stages\]

The highlighted box is the one idea this spec adds to ordinary arcade steering: the player sets a point on the track, not a wheel angle.

1. `CheckGround`: raycast down from the sphere; store whether the kart is grounded and the ground normal.
2. `KartAimPoint.UpdateAim`: smooth the aim yaw from the steer input, cast the aim ray, store the aim point or the fallback.
3. `AlignModelToGround`: tilt the model so its up matches the ground normal.
4. `TurnTowardAimPoint`: compute curvature to the aim point and rotate the model by speed × curvature.
5. `DriveAlongHeading`: apply throttle and brake to forward speed, kill sideways velocity with grip, write `linearVelocity`.
6. Add extra downward acceleration so the sphere stays on the track over crests.
7. `LateUpdate`: move the visual model to the sphere's interpolated position.

## Acceptance tests and risks

Run these on a flat test track with the start values, in play mode with the aim gizmo visible. The numbers follow from the start values, so a large mismatch means a bug, not bad tuning.

- [ ] The project compiles in Unity 6.6 with no use of `UnityEngine.Input`, `Rigidbody.velocity` or `Rigidbody.drag`.
- [ ] With no input, the kart stays still; it never drifts or turns on its own.
- [ ] Hold Accelerate only: the kart reaches about 24 m/s within 5 seconds and drives straight.
- [ ] Release all input at top speed: the kart coasts to a stop in about 6 seconds, keeping its heading.
- [ ] Hold Accelerate and Steer right at low speed: the kart arcs right; at higher speed the arc is visibly wider.
- [ ] Release Steer: the aim ray returns to straight ahead within about half a second and the kart holds its new heading.
- [ ] Hold Brake at top speed: the kart stops in about 1 second, then reverses up to 8 m/s while Brake stays held.
- [ ] Hold Accelerate and Brake together while moving: the kart stops and does not reverse.
- [ ] Steer while stationary: the kart does not turn.
- [ ] Drive off the edge of the track: the aim gizmo turns red, the kart keeps a valid target, and the console has no NaN or exception.
- [ ] Drive up and over a 20-degree ramp: the model tilts with the surface and the sphere keeps ground contact on the crest.
- [ ] The same three actions work on a gamepad, a generic joystick and the arrow keys.

* [ ] The aim reticle is visible from the first frame, sits on the track surface, and turns red when the ray misses.
* [ ] Raising `steerSensitivity` or lowering `steerResponseExponent` makes the same stick movement turn the kart more.

**Known risks**

- **Feel.** Held stick gives a constant arc. If playtesters find it floaty compared with a direct steering angle, use the fallback in Rejected options.
- **Sphere on mesh seams.** A developer making a sphere-based kart in Godot [reported](https://itch.io/devlog/373320/kart-controller.amp) that round bodies did not roll smoothly over joins in static meshes. This spec freezes sphere rotation and uses a frictionless material to reduce that risk, but test on real `MeshCollider` tracks early.
- **Alpha APIs.** The 6.6 docs were labelled Alpha when I searched. Re-check anything that fails to compile against the installed editor.
- **Uncompiled code.** The scripts were written without a compiler; the first job is to compile and run the tests above.
- **Out of scope.** Drift, boost, items, AI karts, networking and camera work. Accelerate plus Brake is reserved for a later drift or hop.

## Rejected options and sources

| Option | Why it is not the method |
| --- | --- |
| Free camera or mouse aim (the original idea) | Needs a mouse or pointer. The brief limits controls to a stick or arrow keys, so the stick moves the aim point instead. |
| Direct stick-to-wheel-angle steering | The simplest arcade scheme and the fallback if the aim point feels floaty: replace `TurnTowardAimPoint` with a turn rate of steer × `maxTurnRateDegreesPerSecond` × speed fraction. It loses the ground-following aim point. |
| `WheelCollider` vehicle | Closer to a driving simulator than to Mario Kart, and more to tune. |
| Click-to-move or NavMesh goal | Built for agents that go to a destination; the player here keeps steering continuously. |
| Gaze or head steering (VR studies) | Relevant research, but the brief is stick and button input. |

**Sources** (search results as of 7 Oct 2026, read through excerpts, not in full)

- [Unity Input System package page for 6000.6](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html)
- [Unity 6.6 Input manual](https://docs.unity3d.com/6000.6/Documentation/Manual/Input.html)
- [Rigidbody guide: Unity 6 renames to linearVelocity and linearDamping](https://uhiyama-lab.com/en/notes/unity/unity-rigidbody-guide/)
- [Input System API: reading action values](https://docs.unity3d.com/Packages/com.unity.inputsystem%401.12/api/UnityEngine.InputSystem.InputAction.CallbackContext.html)
- [Input System bindings: composites](https://docs.unity3d.com/Packages/com.unity.inputsystem@0.2/manual/ActionBindings.html)
- [Pure Pursuit Algorithm explainer](https://av2.readthedocs.io/en/latest/theory/pure-pursuit.html)
- [Steering Behaviors lecture slides (Reynolds' seek and arrive)](https://diana.ms.mff.cuni.cz/pogamut_files/lectures/2015-2016/steeringBehaviors-slides.pdf)
- [gamedev.net: Arcade car physics for a Mario Kart-style game](https://gamedev.net/forums/topic/699625-arcade-car-physics/5394776/)
- [Retro Karting League devlog: sphere kart controller](https://itch.io/devlog/373320/kart-controller.amp)
- [Panda3D manual: raycast vehicles](https://docs.panda3d.org/1.9/cpp/programming/physics/bullet/vehicles)
