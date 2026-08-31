using UnityEngine;
using KartAcademy.Core;

public class SurfaceProvider : MonoBehaviour
{
    [SerializeField] private KartSurfaceConfig surfaceConfig;
    
    public KartSurfaceConfig SurfaceConfig => surfaceConfig;
}