# Kart Guide: from Blender to a working prefab

You hand in **one folder** (`Lastname_Firstname_Kart`) with a prefab that works when it's dragged into the kart roster. Nothing outside your folder may change.

## 0. The rules the game expects

- **Wheels belong to code.** The game spins and steers the wheels itself. Don't animate the wheel bones; any keys on them are ignored.
- **Everything else belongs to your Animator.** The game sends these parameters to it (use the ones you need, names must match exactly):

| Parameter | Type | Value |
|---|---|---|
| `SpeedFraction` | Float | 0 standing still, 1 at top speed |
| `Steer` | Float | -1 full left, +1 full right |
| `Speed` | Float | m/s, negative when reversing |
| `IsDriving` | Bool | false on the selection screen and during the countdown |
| `IsGrounded` | Bool | false in the air |
| `IsBoosting` | Bool | true on a boost |
| `Respawn` / `Respawned` | Trigger | start / end of a respawn |

## 1. Blender

**Model**
- The kart faces **-Y** (Front view, numpad 1, shows the front). Origin on the ground, centred between the wheels. Use the template FBX as a size reference.
- Apply all transforms (Ctrl+A > All Transforms) on the mesh and the armature.

**Rig**
- One armature. One bone per wheel, with only that wheel weighted to it (100%).
- Wheel bones: **head at the hub centre, bone pointing sideways along the axle** (outwards is fine, left and right may mirror). Roll doesn't matter.
- Any other bones (body, engine, driver...) are yours to animate.

**Animations**: **one armature, several actions.** Don't duplicate the kart per animation: a copy gets renamed `Armature.001`, and Unity then can't find any of its bones (the clip plays nothing). Create each action in the Action Editor on the same armature and click the **shield icon (Fake User)** on each, or Blender deletes unused actions when you save. First frame = last frame so they loop.
- `Idle`: standing still, engine running.
- `Racing`: driving at full speed.
- `LeanLeft` / `LeanRight`: driving while steering hard.

**Export: model** → `SKM_Kart_Lastname.fbx`
- Select the mesh + armature. *Limit to: Selected Objects*. *Object Types: Armature, Mesh*.
- *Apply Scalings: FBX All*. Forward *-Z*, Up *Y*.
- *Armature*: Primary Bone Axis **Y**, Secondary **X**, FBXNode Type **Null**, *Only Deform Bones* off, *Add Leaf Bones* **off**.
- *Animation* panel: header checkbox **off**.

**Export: each animation** → `Anim_Kart_Lastname_Idle.fbx`, `..._Racing.fbx`, `..._LeanLeft.fbx`, `..._LeanRight.fbx`
- Set the action as the active action of the armature (Action Editor).
- Select **only the armature**, *Object Types: Armature* only. The mesh isn't needed: the clips drive bones, and the mesh comes from your model FBX. (Exception: if you animate shape keys, include the mesh.)
- Don't rename the armature object or any bone between exports. Unity finds bones by their path, for example `Armature/Body/Engine`. If a clip plays nothing in Unity, open it in the Animation window: yellow "(Missing!)" curves mean the names don't match.
- Same settings as the model, but the *Animation* panel header checkbox **on**, with:
  - *Key All Bones* **on**, *Force Start/End Keying* **on**
  - *NLA Strips* **off**, *All Actions* **off** (otherwise every action ends up in every file)
  - *Sampling Rate* 1, *Simplify* 1

## 2. Unity: start your folder

1. Duplicate `Assets/Karts/Name_Template_Kart` and rename it `Lastname_Firstname_Kart`.
2. Rename the prefab to `ClassCode_Lastname_Firstname_Kart` (like the template).
3. Put your files in: FBX model in `3D`, animation FBXs in `Anim`, textures in `Tex`, materials in `Mat`. Delete the template's FBXs from your copy.

## 3. Unity: import settings (Inspector, then **Apply**)

**Model FBX**
- *Rig*: Animation Type **Generic**, Avatar Definition **Create From This Model**.
- *Animation*: Import Animation **off**.
- *Materials*: Material Creation Mode **None** (you make your own).

**Each animation FBX**
- *Rig*: Animation Type **Generic**, Avatar Definition **Copy From Other Avatar**, Source = your model's avatar.
- *Animation*: select the clip, rename it (Blender names it after the scene, e.g. `Scene`) to `Idle`, `Racing`... and tick **Loop Time**. Leave *Loop Pose* off: your first and last frame are already identical. Scrub the preview past the end to check the loop has no jump.
- The clip preview at the bottom is empty because the file has no mesh. Drag your model FBX onto the preview window to see the animation on your kart.

**Textures**
- BaseMap, Emissive: default.
- MaskMap: **sRGB off**.
- Normal: Texture Type **Normal map**.

## 4. Unity: material (HDRP/Lit)

Create `M_Lastname` in `Mat`, shader **HDRP/Lit**:
- *Base Map* → BaseMap
- *Mask Map* → MaskMap (R metallic, G ambient occlusion, B detail mask, A smoothness)
- *Normal Map* → Normal
- *Emission*: Emissive Color map → Emissive, set the colour to white and raise the intensity until it glows.

## 5. Unity: Animator Controller

1. In `Anim`: Create > Animator Controller → `AC_Kart_Lastname`.
2. Open it. In *Parameters* add `SpeedFraction` (Float) and `Steer` (Float).
3. Right-click the grid > Create State > **From New Blend Tree**. Double-click it.
4. Blend Type **2D Freeform Cartesian**, parameters `SpeedFraction` and `Steer`.
5. Add motions and positions:

| Clip | Pos X (SpeedFraction) | Pos Y (Steer) |
|---|---|---|
| Idle | 0 | 0 |
| Racing | 1 | 0 |
| LeanLeft | 1 | -1 |
| LeanRight | 1 | 1 |

6. Test it: drag the sliders in the blend tree preview.

## 6. Unity: the prefab

Open your prefab (double-click):
1. Under `KartModel`, delete `BodyPlaceholder`, `FrontLeftSteerPivot`, `FrontRightSteerPivot`, `RearLeftWheel`, `RearRightWheel`.
2. Drag your model FBX **under `KartModel`**. Position and rotation 0,0,0. Assign your material to its mesh.
3. On your model object (not `KartModel`!), the **Animator**: Controller = your controller, Avatar = your avatar, **Apply Root Motion off**.
4. On the prefab root, **Kart Wheel Visuals**:
   - Drag your four wheel bones into the four *Wheel* slots.
   - *Wheel Radius*: your wheel's radius in metres.
5. On the prefab root: Add Component > **Kart Animator Parameters**. Drag your Animator into its slot.
6. Save the prefab.

## 7. Test

1. Open `Assets/Kart-Data/Data/KartRoster.asset`, add an entry, drag your prefab in.
2. Play `Assets/Menu-Scenes/SelectionScreen.unity`: your kart idles.
3. Pick it and drive: it blends into Racing, leans when steering, the wheels roll forward and steer.

**Checklist before handing in:** wheels roll the right way, kart sits on the road (not floating or sinking), no errors in the Console, everything is inside your own folder.
