using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.UI;
using UnityEngine.Rendering;
using System;

[Serializable]
public class Wheel
{
    public Boolean accelerates;
    public Boolean steers;
    public Transform anchor;
    public GameObject wheelMesh;
    public float radius = 0.15f;
    public bool isGrounded=false;
}

public class kartHandlerPhysics : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField] List<Wheel> wheels = new List<Wheel>();
    [SerializeField] float hoverForce = 65f;
    [SerializeField] float hoverHeight = 0.5f;
    [SerializeField] float acceleration = 50f;
    [SerializeField] float maxSpeed = 400f;
    [SerializeField] float maxDownWardForce = 100f;
    [SerializeField] float damping = 100f;
    [SerializeField] float steerAngle = 30f;
    [SerializeField] float turnSpeed = 5f;
    [SerializeField] float slip = 10f;
    [SerializeField] float windDragFactor = 0.05f;
    [SerializeField] float windDragMaximum = 1f;
    [SerializeField] float spinDragFactor = 1f;
    [SerializeField] float spinDragMaximum = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        HandleAcceleration();
        HandleFloat();
        HandleSteering();
         
    }

    private void HandleAcceleration()
    {
        float moveInput = Input.GetAxis("Vertical");
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        if (Mathf.Abs(moveInput) < 0.1 || localVelocity.magnitude > maxSpeed)
        {
            //rb.linearVelocity *= 0.8f; // doesnt work, use drag instead
        }
        else
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.accelerates && wheel.isGrounded)
                {
                    // Apply force at the wheel position for better physics interaction
                    Vector3 force = wheel.anchor.forward * moveInput * acceleration;
                    rb.AddForceAtPosition(force, wheel.anchor.position, ForceMode.Force);
                }
            }
        }

        float forwardSpeed = Math.Abs(localVelocity.z);

        // Apply downforce based on speed
        float downforce = Mathf.Clamp(forwardSpeed, 0, maxDownWardForce);
        rb.AddForce(-transform.up * downforce);

        // Apply drag based on speed
        float drag = Mathf.Clamp(forwardSpeed * windDragFactor, 0, windDragMaximum);
        rb.linearDamping = drag;

        // Apply angular drag based on speed
        float angularDrag = Mathf.Clamp(forwardSpeed * spinDragFactor, 0, spinDragMaximum);
        rb.angularDamping = angularDrag;
    }

    private void HandleFloat()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.isGrounded = false;
            RaycastHit hit;
            if (Physics.Raycast(wheel.anchor.position, -wheel.anchor.up, out hit, hoverHeight*2))
            {
                wheel.isGrounded = true;

                float compression = Mathf.Clamp01((hoverHeight - hit.distance) / hoverHeight);

                // Damping: oppose motion along the up-axis of the point
                float upVel = Vector3.Dot(rb.GetPointVelocity(wheel.anchor.position), hit.normal);
                float damp = upVel * damping;

                Vector3 force = hit.normal * (compression * hoverForce - damp);
                rb.AddForceAtPosition(force, wheel.anchor.position, ForceMode.Force);

                wheel.wheelMesh.transform.position = hit.point + hit.normal * wheel.radius;
            }
        }
    }

    private void HandleSteering()
    {
        float moveInput = Input.GetAxis("Vertical");

        foreach (Wheel wheel in wheels)
        {
            if (wheel.steers && wheel.isGrounded)
            {
                // Calculate target rotation around the local Y-axis
                float targetAngle = Input.GetAxis("Horizontal") * steerAngle;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

                // Smoothly interpolate from current to target rotation
                wheel.anchor.localRotation = Quaternion.Slerp(
                    wheel.anchor.localRotation,
                    targetRotation,
                    Time.fixedDeltaTime * turnSpeed
                );
            }

            // Apply lateral slip force
            Vector3 wheelWorldPos = wheel.anchor.position;
            Vector3 wheelRight = wheel.anchor.right;

            // Get velocity at wheel position
            Vector3 velocity = rb.GetPointVelocity(wheelWorldPos);

            // Project velocity onto wheel's right vector to get lateral velocity
            float lateralVelocity = Vector3.Dot(wheelRight, velocity);

            // Calculate and apply lateral force
            Vector3 lateralForce = -wheelRight * lateralVelocity * slip;
            rb.AddForceAtPosition(lateralForce, wheelWorldPos);

        }

    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (wheels == null) return;

        float maxDist = hoverHeight * 2f;

        foreach (var wheel in wheels)
        {
            if (wheel == null) continue;

            Vector3 origin = wheel.anchor.position;
            Vector3 dir = -wheel.anchor.up;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDist))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.03f);

                Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
                Gizmos.DrawLine(hit.point, origin + dir * maxDist);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + dir * maxDist);
            }
        }
    }
#endif

}
