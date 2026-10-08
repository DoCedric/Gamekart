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
