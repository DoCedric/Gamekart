#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates starter SurfaceType assets so they don't have to be made by hand.</summary>
public static class SurfaceTypeMaker
{
    const string Folder = "Assets/Kart/Data/Surfaces";

    [MenuItem("Kart/Create Default Surface Types")]
    public static void CreateDefaults()
    {
        Directory.CreateDirectory(Folder);

        Create("Asphalt", 1f, 1f, 1f);
        Create("Grass", 0.6f, 0.7f, 0.6f);
        Create("Ice", 1f, 0.5f, 0.15f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Create(string assetName, float maxSpeed, float acceleration, float grip)
    {
        string path = $"{Folder}/{assetName}.asset";
        if (AssetDatabase.LoadAssetAtPath<SurfaceType>(path) != null) return; // never overwrite tweaked assets

        SurfaceType surface = ScriptableObject.CreateInstance<SurfaceType>();
        surface.maxSpeedMultiplier = maxSpeed;
        surface.accelerationMultiplier = acceleration;
        surface.gripMultiplier = grip;
        AssetDatabase.CreateAsset(surface, path);
    }
}
#endif
