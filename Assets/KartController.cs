using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class KartController : MonoBehaviour
{
    public float maxSpeed = 100f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    public float turnSpeed = 80f;

    public Transform[] wheelGroundContactBones; // Array to hold the wheel bones
    public float wheelOffset = 0.1f;
    public float fallingSpeed = 120f;
    public float hitDampeningFactor = .3f;

    public float raycastDistance = 4f; // Distance to check for the track surface
    public float wheelheightCastDistance = 4f; // Distance to check for the track surface
    public float gimbalSpeed = 10f; // Speed to align the kart with the track

    public LayerMask trackLayer; // Layer mask to identify the track
    public LayerMask obstacleLayer; // Layer mask to identify obstacles
    public LayerMask deadzoneLayer; // Layer mask to identify the deadzone

    private float currentSpeed = 0f;
    private BoxCollider boxCollider;

    // Respawn position and rotation
    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    [SerializeField] private Animator anim;
    private CharacterController cc_controller;

    void Start()
    {


        //get the start loc and rot
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;

        cc_controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        //respawn if falls through world
        if (cc_controller.transform.position.y < -20)
        {
            respawn();
        }

        // Get input from WASD keys
        float moveDirection = Input.GetAxis("Vertical"); // W and S keys
        float turnDirection = Input.GetAxis("Horizontal"); // A and D keys

        // Accelerate or decelerate based on input
        if (moveDirection > 0)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        else if (moveDirection < 0)
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }
        else
        {
            // Gradually slow down when no input is given
            if (currentSpeed > 0)
            {
                currentSpeed -= deceleration * Time.deltaTime;
            }
            else if (currentSpeed < 0)
            {
                currentSpeed += deceleration * Time.deltaTime;
            }
        }

        // Clamp the speed to the max speed
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // Forward, backward and steering
        cc_controller.Move(transform.forward * currentSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, turnDirection * turnSpeed * Time.deltaTime);

        // Raycast to detect the track surface below the kart for position
        Vector3 averageNormal = Vector3.zero;
        Vector3 averagePosition = Vector3.zero;
        int hitCount = 0;

        foreach (Transform wheelBone in wheelGroundContactBones)
        {
            RaycastHit hit;
            if (Physics.Raycast(wheelBone.position + ((wheelBone.forward * -1f) * wheelheightCastDistance), (wheelBone.forward), out hit, raycastDistance, trackLayer))
            {
                averageNormal += hit.normal;
                averagePosition += hit.point;
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            averageNormal /= hitCount;
            float angleDifference = Vector3.Angle(transform.up, averageNormal);
            float adjustedGimbalSpeed = gimbalSpeed * (angleDifference / 90f);

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageNormal) * transform.rotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * adjustedGimbalSpeed);

            averagePosition /= hitCount;
            transform.position = averagePosition;
        }
        else
        {
            //transform.position -= Vector3.down * fallingSpeed * Time.deltaTime;
        }

        //update animations
        UpdateAnimations(moveDirection, turnDirection);
    }

    void UpdateAnimations(float moveDir, float turnDir)
    {
        if (anim != null)
        {
            // Update animations based on the current speed
            anim.SetFloat("Steering", turnDir);
            anim.SetFloat("Gas", moveDir);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log("I hit somthinge");
        currentSpeed *= -hitDampeningFactor;
}

    void respawn()
    {
        // Respawn the kart at the specified position and rotation
        cc_controller.enabled = false; // Disable the controller temporarily
        transform.position = respawnPosition;
        transform.rotation = respawnRotation;
        currentSpeed = 0f; // Reset speed
        cc_controller.enabled = true; // Re-enable the controller
    }
}
