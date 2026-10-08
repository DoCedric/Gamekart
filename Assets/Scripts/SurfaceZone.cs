using UnityEngine;

/// <summary>
/// Put on a track object (or a parent of its collider) to define the surface
/// the kart is driving on while the ground check hits that collider.
/// </summary>
public class SurfaceZone : MonoBehaviour
{
    [SerializeField] SurfaceType surfaceType;

    public SurfaceType SurfaceType => surfaceType;
}
