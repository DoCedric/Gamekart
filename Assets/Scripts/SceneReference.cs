using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Lets you drag a scene into the Inspector instead of typing its name.
/// The path is stored for runtime, so renaming or moving the scene is safe.
/// The scene must still be in the Build Profile's scene list.
/// </summary>
[Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
#if UNITY_EDITOR
    [SerializeField] SceneAsset sceneAsset;
#endif
    [SerializeField, HideInInspector] string scenePath;

    public string ScenePath => scenePath;

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        scenePath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty;
#endif
    }

    public void OnAfterDeserialize() { }
}
