# Kart2627 Design Doc

A short, abstract overview of what the game does today and how the pieces fit together.
It describes behaviour, not code. Class names are given so you can find the real thing.

## 1. Game flow

1. **Selection scene.** The player browses the available karts, shown as rotating display models, and confirms one.
2. **Hand-over.** The chosen kart prefab is stored in a static holder (`KartSelection`) and the track scene is loaded.
3. **Track scene.** A spawn point creates the chosen kart, AI karts are spawned on their grid slots, a countdown runs, then the race starts.
4. **Race.** Laps, checkpoints, times and positions are tracked. The player finishes after the final lap.
5. **Return.** A dedicated input clears the selection and loads the selection scene again (`ReturnToSelection`).

Scenes are referenced through a small `SceneReference` wrapper, so they can be dragged into inspector fields.

## 2. Kart driving

### Core idea
Every kart is a **single rolling sphere** (the *SphereBody*, a rigidbody) plus a separate visual **KartModel**. The sphere does all the physics. The model is placed under the sphere every frame and turned to match the ground and the kart's heading. There are no wheel colliders, and the wheel meshes are cosmetic only (`KartWheelVisuals`).

### Control chain
```
Input source  ->  Aim point  ->  Pure-pursuit steering  ->  Motor  ->  Sphere velocity
```
- **Input source (`KartInputSource`)** is an abstract provider of three values: steer (-1..1), accelerate (0..1) and brake/reverse (0..1). The player's `KartInputReader` and the `KartAIDriver` both implement it, so the motor can't tell them apart.
- **Aim point (`KartAimPoint`)** turns the steer value into a yaw offset in front of the kart. A ray is cast from there onto the track, and the hit point is the point the kart steers toward. The allowed yaw shrinks as speed grows.
- **Motor (`KartMotor`)** steers by pure pursuit toward the aim point, then applies throttle, braking, reverse, sideways grip and an extra downward force. It uses a ground raycast to know whether the kart is grounded and to read the surface and ground normal.
- **Tuning (`KartTuning`)** is a ScriptableObject with every handling number (speeds, acceleration curve, aim ray values, turn rate, grip, ground layers, boost decay). Several karts can share one asset.

### Per-kart scaling
`KartMotor` has `TopSpeedScale` and `AccelerationScale` (default 1) on top of the shared tuning. The AI uses these for difficulty. They are not stored in the tuning asset, because that asset is shared.

### Size
The sphere's radius is **1 m** on every kart. The motor's `sphereRadius` and `modelHeightBelowSphereCenter` must match it (the KartMaker sets all three from one constant). Kart-vs-kart collision is sphere vs sphere only. A long kart's body can visually overlap another kart.

## 3. Surfaces

- **`SurfaceType`** is an asset with three multipliers: top speed, acceleration and grip.
- **`SurfaceZone`** goes on a track object or a parent of its collider. When the ground ray hits that collider, the zone's surface applies.
- With no zone, the motor's **default surface** is used.
- The motor **blends** toward the new surface's multipliers over time, so crossing an edge doesn't snap.
- A terrain is one collider, so it carries one surface. Detecting painted terrain layers is a designed but unbuilt feature (see section 13).

## 4. Kart selection and roster

- **`KartRoster`** is the one list of karts (prefab and icon per entry). The selection screen and the AI spawner both read it.
- **`selector`** cycles through the roster, shows the kart (with driving disabled) and displays its cleaned-up name. It auto-advances on a timer, which can be toggled. The display kart can be dragged to rotate or quick-spun (`KartSelectorSpin`).
- **`KartDrivingState`** switches a kart between *display* (no physics, input or aim reticle) and *driving*.

## 5. Race system

### Checkpoints
- A **`Checkpoint`** is an invisible box trigger stretched across the full road width. It reports which rigidbody crossed it and does nothing else.
- The designer places them as **children of the `RaceManager`**. Hierarchy order is race order, and the **first child is the start/finish line**.

### `RaceManager`
The single authority for the race. It owns the checkpoint list, lap count, race state and clock, and a `RacerState` for every registered kart.

- **Registration.** Every kart (player and AI) gets a `Racer` component and registers. The manager holds karts still until the start signal.
- **Rules for a lap.**
  - The first forward crossing of the finish line starts lap 1.
  - Checkpoints must be crossed in order. Out-of-order gates and crossings made backwards are ignored.
  - Crossing the finish line with every checkpoint done completes the lap and records its time.
  - Crossing it with checkpoints missing resets that lap's checkpoint progress. The lap number stays and the lap clock keeps running.
  - Completing the final lap finishes the racer.
- **States.** Countdown (karts held, clock at zero), then Racing, then Finished (when everyone has finished).
- **Countdown.** It counts 3, 2, 1, then GO. At GO, driving is enabled for all racers and the race clock starts.
- **Positions.** Ranked every frame: finishers first (by finish time), then lap, then progress through the lap, then distance to the next gate.
- **After finishing.** A finished racer's result is frozen, but its next gate keeps advancing, so a kart that keeps driving (an AI) always has a target.
- **Events** for UI and effects: countdown changes, state changes, racer registered, lap started, lap completed, progress reset, racer finished, all finished.

## 6. AI

### Spawning (`AiSpawner`)
- The designer adds empty children to the spawner. One AI kart is spawned per child, using its position and forward direction.
- Each kart is a random prefab from the roster. The player's input and aim reticle are removed and an AI driver, a `Racer` and a `KartRespawner` are added before the kart wakes up.

### Driving (`KartAIDriver`)
- It only produces the three input values, so it drives through exactly the same motor and physics as the player.
- **Target.** It aims across the next checkpoint, at a lateral position that is unique per AI (its *line*), leaning toward the gate after next as it closes in.
- **Opening.** On the grid each AI drives straight down its own lane (with a little drift), then settles onto its personal line over the first few gates.
- **Steering** is computed by inverting the aim point's steer shaping, so the kart's aim point lands on the target.
- **Speed.** Full speed on straights (with a small random per-AI slowdown), slower for sharp corners, and a little slower when steering hard. It brakes only when clearly too fast, and not while boosting.
- **Stuck handling.** If it barely moves for a few seconds after the opening, it asks for a respawn.
- **After finishing.** It keeps driving.

### Settings (`AiDriverSettings`)
One asset holds all AI behaviour, including **difficulty** (top speed and acceleration scale, e.g. 1.2 = 120%). It is assigned to the AI spawner. Making one asset per difficulty is the intended use.

## 7. Respawn (`KartRespawner`)

- Works on any kart. Moves it back to the **last checkpoint passed** (or its starting spot), facing the driving direction, dropped onto the road.
- **Sequence:** freeze, `RespawnStarted` event, wait for the respawn delay (the room for a vanish animation), move, unfreeze, `Respawned` event.
- Per-kart events and static `Any...` events are provided, so effects or sounds can subscribe from one scene object.

## 8. Boost pads (`BoostPad`)

- An invisible box trigger with one setting: **speed percent** of the kart's top speed.
- A kart entering it is lifted to that speed (100 = top speed, 150 = one and a half times it). A kart that is already faster is not slowed.
- The motor raises its speed limit temporarily, and that limit decays at a rate set in `KartTuning`. The kart slows back down on its own.
- Works on any surface. It raises a `Triggered` event for effects.

## 9. Camera (`KartCameraFollow`)

- A smoothed third-person follow camera. It has a **position offset** relative to the kart and a separate **look-at offset**, so the camera can aim above the kart.
- Its target is set by code when the player's kart spawns. There is no target field in the inspector.

## 10. HUD (UI Toolkit)

- Built with a **PanelRenderer**, `UID_Race.uxml` and `UID_Style.uss`.
- **`RaceHud`** reads the player's `RacerState` and the `RaceManager`. It never changes the race.
- Labels: **position** (`3/6`), **lap** (`LAP 2/3`), **time** (current lap, `mm:ss:cs`), and a big centre label used for the countdown, `START!` and `FINISH!!`.
- The centre label animates through USS classes. A "start" class is applied for a frame and then removed, so a USS transition plays back to the resting look.

## 11. Editor tools

- **Kart > Build Kart** builds a kart prefab with the standard hierarchy and components.
- **Kart > Create Default Surface Types** creates starter surface assets.
- Scene-view gizmos: **green** player spawn, **magenta** AI slots, **cyan/yellow** checkpoints (yellow when selected, with arrow and index), **orange** boost pads.
- A checkpoint's **Debug Gizmo** option draws it in the Game view (yellow, then green once crossed).

## 12. Designer checklist for a new track

1. Build the track with colliders on the track layer (mesh or terrain).
2. Add an empty with **`RaceManager`**. Set the lap count.
3. Add **`Checkpoint`** children in driving order. The first is the finish line. Stretch each box over the full road width, with its blue arrow pointing in the driving direction. Add more gates on bends for a better AI line.
4. Add a **`KartSpawnPoint`** (assign the camera follow) before the finish line.
5. Add an empty with **`AiSpawner`**. Assign the roster and an AI settings asset. Add child empties for the grid slots (about 1 m above the ground).
6. Optionally add **`SurfaceZone`**s for grass, sand and so on, and **`BoostPad`**s.
7. Add the HUD object (PanelRenderer, `RaceHud`).

## 13. Known limitations and ideas

- **No victory scene yet.** After the race the player keeps control and AI karts keep lapping.
- **Painted terrain layers aren't detected.** The proposed `TerrainSurfaceZone` would map terrain layers to surface types.
- **Kart-vs-kart collision is spheres only.** Long karts can visually overlap.
- **The AI does not avoid other karts** and does no rubber-banding.
- **Distance-based ranking** uses straight-line distance to the next gate, so it is approximate between gates.
- **Spawn height.** Spawn empties must clear the ground by about the sphere radius, or the kart pops when released.
