#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor-only utility that builds just the Kart object described in the
/// "Aim-Point Kart Controller: Unity 6.6 Implementation Spec" (Assets/Design)
/// and drops it into whatever scene you currently have open, as well as
/// saving it as a reusable prefab. It does not touch the track, lighting or
/// camera - author those yourself and place the kart on your own track.
///
/// Building this through code (rather than hand-authoring a prefab/scene
/// file) guarantees every reference and layer is wired correctly, including
/// the private [SerializeField] references the spec's scripts expose
/// instead of public fields.
/// </summary>
public static class KartMaker
{
    const string PrefabPath = "Assets/Kart/Prefabs/Kart.prefab";
    const string TuningPath = "Assets/Kart/Data/DefaultKartTuning.asset";
    const string SlipperyMaterialPath = "Assets/Kart/Data/KartSphereSlippery.physicsMaterial";
    const string InputActionsPath = "Assets/Kart/Input/KartControls.inputactions";

    const string TrackLayerName = "Track";
    const string KartLayerName = "Kart";

    [MenuItem("Kart/Build Kart")]
    public static void BuildKartInScene()
    {
        if (!EditorUtility.DisplayDialog("Build Kart",
            "This adds a ready-to-drive Kart to the current scene and saves it as a reusable prefab at " + PrefabPath + ". It also ensures the Track/Kart layers exist and sets Fixed Timestep to 60 Hz project-wide, since the controller depends on both. It does not build a track, lighting or camera - put the kart on your own track and point your own camera at its KartModel child.",
            "Build", "Cancel"))
            return;

        EnsureLayer(TrackLayerName);
        EnsureLayer(KartLayerName);
        SetFixedTimestep(1f / 60f);

        Directory.CreateDirectory("Assets/Kart/Data");
        Directory.CreateDirectory("Assets/Kart/Prefabs");

        GameObject kart = BuildKart();

        PrefabUtility.SaveAsPrefabAssetAndConnect(kart, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = kart;
        SceneView.lastActiveSceneView?.FrameSelected();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Kart added to the scene and saved as a prefab at " + PrefabPath + ". Drag it onto your track, then assign your own KartTuning asset if you want a different feel.");
    }

    // --- One-time project setup the controller depends on -----------------

    static void EnsureLayer(string layerName)
    {
        Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (tagManagerAsset == null) return;

        SerializedObject tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Layers 0-7 are Unity's reserved built-in layers; user layers start at 8.
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                return; // already exists
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning("KartMaker: no free layer slot to add layer '" + layerName + "'.");
    }

    static void SetFixedTimestep(float fixedDeltaTime)
    {
        Object timeManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset").FirstOrDefault();
        if (timeManagerAsset == null) return;

        SerializedObject timeManager = new SerializedObject(timeManagerAsset);
        timeManager.FindProperty("Fixed Timestep").floatValue = fixedDeltaTime;
        timeManager.ApplyModifiedProperties();
    }

    // --- Kart ----------------------------------------------------------

    static GameObject BuildKart()
    {
        int kartLayer = LayerMask.NameToLayer(KartLayerName);

        // Root never moves or rotates - it only hosts the logic components.
        // SphereBody and KartModel (added below) are the two children that
        // actually move, per the spec's kart hierarchy.
        GameObject kart = new GameObject("Kart");
        kart.layer = kartLayer;
        kart.transform.position = Vector3.up; // 1m up, so it settles onto whatever track you drag it onto.

        Rigidbody sphereBody = BuildSphereBody(kart.transform, kartLayer);
        Transform kartModel = BuildKartModel(kart.transform, kartLayer);

        KartTuning tuning = LoadOrCreateTuning();

        KartInputReader inputReader = kart.AddComponent<KartInputReader>();
        AssignInputActions(inputReader);

        KartAimPoint aimPoint = kart.AddComponent<KartAimPoint>();
        SetPrivateFields(aimPoint,
            ("tuning", tuning),
            ("sphereBody", sphereBody),
            ("kartModel", kartModel));

        KartMotor motor = kart.AddComponent<KartMotor>();
        SetPrivateFields(motor,
            ("tuning", tuning),
            ("sphereBody", sphereBody),
            ("kartModel", kartModel));
        SetPrivateFloat(motor, "sphereRadius", 0.5f);
        SetPrivateFloat(motor, "modelHeightBelowSphereCenter", 0.5f);

        BuildAimReticle(kart.transform, aimPoint);
        BuildWheelVisuals(kart, kartModel, kartLayer, motor, aimPoint);

        return kart;
    }

    static Rigidbody BuildSphereBody(Transform parent, int kartLayer)
    {
        GameObject go = new GameObject("SphereBody");
        go.layer = kartLayer;
        go.transform.SetParent(parent, false);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // stops tunnelling at 24 m/s, per the spec.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        SphereCollider collider = go.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        collider.sharedMaterial = LoadOrCreateSlipperyMaterial();

        return rb;
    }

    static Transform BuildKartModel(Transform parent, int kartLayer)
    {
        // Deliberately left at the default scale (1,1,1) and identity
        // rotation: KartMotor positions/rotates this transform directly
        // every frame, and anything you parent under it - your own kart
        // mesh in place of BodyPlaceholder, extra visual bits, whatever -
        // inherits that clean transform instead of being stretched by a
        // non-uniform parent scale.
        GameObject modelGO = new GameObject("KartModel");
        modelGO.layer = kartLayer;
        modelGO.transform.SetParent(parent, false);

        BuildBodyPlaceholder(modelGO.transform, kartLayer);

        return modelGO.transform;
    }

    static void BuildBodyPlaceholder(Transform kartModel, int kartLayer)
    {
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = "BodyPlaceholder";
        placeholder.layer = kartLayer;
        placeholder.transform.SetParent(kartModel, false);
        placeholder.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        placeholder.transform.localScale = new Vector3(1.4f, 0.5f, 2f);

        // Visual only - the SphereBody's collider handles all collision.
        // Delete this object (or just its mesh/renderer) once you parent
        // your own kart body mesh under KartModel.
        Object.DestroyImmediate(placeholder.GetComponent<Collider>());
    }

    static void BuildWheelVisuals(GameObject kart, Transform kartModel, int kartLayer, KartMotor motor, KartAimPoint aimPoint)
    {
        Transform frontLeftPivot = CreateWheelPivot(kartModel, kartLayer, "FrontLeftSteerPivot", new Vector3(-0.7f, 0.35f, 1f));
        Transform frontLeftWheel = CreateWheelMesh(frontLeftPivot, kartLayer, "FrontLeftWheel", Vector3.zero);

        Transform frontRightPivot = CreateWheelPivot(kartModel, kartLayer, "FrontRightSteerPivot", new Vector3(0.7f, 0.35f, 1f));
        Transform frontRightWheel = CreateWheelMesh(frontRightPivot, kartLayer, "FrontRightWheel", Vector3.zero);

        Transform rearLeftWheel = CreateWheelMesh(kartModel, kartLayer, "RearLeftWheel", new Vector3(-0.7f, 0.35f, -1f));
        Transform rearRightWheel = CreateWheelMesh(kartModel, kartLayer, "RearRightWheel", new Vector3(0.7f, 0.35f, -1f));

        KartWheelVisuals wheelVisuals = kart.AddComponent<KartWheelVisuals>();
        SetPrivateFields(wheelVisuals,
            ("motor", motor),
            ("aimPoint", aimPoint),
            ("frontLeftSteerPivot", frontLeftPivot),
            ("frontLeftWheelMesh", frontLeftWheel),
            ("frontRightSteerPivot", frontRightPivot),
            ("frontRightWheelMesh", frontRightWheel),
            ("rearLeftWheelMesh", rearLeftWheel),
            ("rearRightWheelMesh", rearRightWheel));
    }

    // An empty, identity-rotation pivot the front wheels steer around.
    // KartWheelVisuals sets its local rotation every frame, so nothing
    // else should rely on a baked-in rotation here.
    static Transform CreateWheelPivot(Transform parent, int layer, string name, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    static Transform CreateWheelMesh(Transform parent, int layer, string name, Vector3 localPosition)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        // Unity's cylinder primitive has its rotational symmetry axis along
        // its own local Y. Rotating it 90 degrees on Z points that axis
        // sideways along the axle direction - which is also the axis
        // KartWheelVisuals spins it around (Space.Self Vector3.up).
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        go.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f); // radius 0.35, thickness 0.3.

        // Visual only - the SphereBody's collider handles all collision.
        Object.DestroyImmediate(go.GetComponent<Collider>());

        return go.transform;
    }

    static void AssignInputActions(KartInputReader inputReader)
    {
        AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath);

        InputActionReference FindAction(string actionName) =>
            subAssets.OfType<InputActionReference>().FirstOrDefault(r => r.action != null && r.action.name == actionName);

        SetPrivateFields(inputReader,
            ("steerAction", FindAction("Steer")),
            ("accelerateAction", FindAction("Accelerate")),
            ("brakeReverseAction", FindAction("BrakeReverse")));
    }

    static void BuildAimReticle(Transform kartRoot, KartAimPoint aimPoint)
    {
        GameObject reticleGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        reticleGO.name = "AimReticle";
        reticleGO.transform.SetParent(kartRoot, false);
        reticleGO.transform.localScale = Vector3.one * 0.6f;
        reticleGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // No collider on the reticle - it's a visual marker only.
        Object.DestroyImmediate(reticleGO.GetComponent<Collider>());

        Renderer reticleRenderer = reticleGO.GetComponent<Renderer>();
        // Sprites/Default is a simple unlit shader with a "_Color" property
        // that renders correctly under any render pipeline (including
        // HDRP), unlike the HDRP/Lit or HDRP/Unlit shaders KartAimReticle's
        // Material.color convenience setter would silently fail to affect.
        reticleRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        KartAimReticle reticle = kartRoot.gameObject.AddComponent<KartAimReticle>();
        SetPrivateFields(reticle,
            ("aimPoint", aimPoint),
            ("reticle", reticleGO.transform),
            ("reticleRenderer", reticleRenderer));
    }

    // --- Shared assets ---------------------------------------------------

    static KartTuning LoadOrCreateTuning()
    {
        KartTuning tuning = AssetDatabase.LoadAssetAtPath<KartTuning>(TuningPath);
        if (tuning == null)
        {
            tuning = ScriptableObject.CreateInstance<KartTuning>();
            AssetDatabase.CreateAsset(tuning, TuningPath);
        }

        tuning.trackLayers = LayerMask.GetMask(TrackLayerName); // must exclude Kart, per the spec.
        EditorUtility.SetDirty(tuning);
        return tuning;
    }

    static PhysicsMaterial LoadOrCreateSlipperyMaterial()
    {
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(SlipperyMaterialPath);
        if (material != null) return material;

        material = new PhysicsMaterial("KartSphereSlippery")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };
        AssetDatabase.CreateAsset(material, SlipperyMaterialPath);
        return material;
    }

    // --- Reflection helpers for the spec's private [SerializeField] fields --

    static void SetPrivateFields(Object target, params (string fieldName, Object value)[] fields)
    {
        SerializedObject so = new SerializedObject(target);
        foreach ((string fieldName, Object value) in fields)
            so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetPrivateFloat(Object target, string fieldName, float value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(fieldName).floatValue = value;
        so.ApplyModifiedProperties();
    }
}
#endif
