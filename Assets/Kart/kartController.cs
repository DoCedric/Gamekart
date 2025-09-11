using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class KartController : MonoBehaviour
{
    public float maxSpeed = 25f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    public float turnSpeed = 50f;

    public Transform[] wheelGroundContactBones; // Array to hold the wheel bones
    public float fallingAcceleration = 20f;
    public float maxFallingSpeed = 200f;
    public float hitDampeningFactor = .3f;
    

    public float raycastDistance = 1.5f; // Distance to check for the track surface
    public float wheelheightCastDistance = 1f; // Distance to check for the track surface
    public float gimbalSpeed = 10f; // Speed to align the kart with the track
    public float heightBeforeFalling = 0f; // Height before the kart starts falling

    public LayerMask trackLayer; // Layer mask to identify the track
    public LayerMask obstacleLayer; // Layer mask to identify obstacles
    public LayerMask deadzoneLayer; // Layer mask to identify the deadzone

    private float currentSpeed = 0f;
    private float currentFallSpeed = 0f;
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
        if (cc_controller.transform.position.y < -200)
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
            else if (currentSpeed < 0.1f && currentSpeed > -0.1f)
            {
                currentSpeed = 0;
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
            if (Vector3.Distance(transform.position, averagePosition) > heightBeforeFalling)
            {
                Debug.Log("FALLING");
                currentFallSpeed = Mathf.Clamp(currentFallSpeed + fallingAcceleration * Time.deltaTime, 0, maxFallingSpeed);
                Vector3 targetPosition = Vector3.MoveTowards(transform.position, averagePosition, Time.deltaTime * currentFallSpeed);
                Vector3 moveVector = targetPosition - transform.position;
                cc_controller.Move(moveVector);
            }
            else
            {
                Debug.Log("Cruising");
                // stick to the track, this is to ensure the kart is only airborn when intended.
                currentFallSpeed = 0;
                transform.position = averagePosition;
            }
        }
        else
        {
            // No track surface detected, apply falling logic
            currentFallSpeed = Mathf.Clamp(currentFallSpeed + fallingAcceleration * Time.deltaTime, 0, maxFallingSpeed);
            cc_controller.Move(Vector3.down * currentFallSpeed * Time.deltaTime); // Move down if no track surface is detected
            
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
        if (((1 << hit.gameObject.layer) & obstacleLayer) != 0)
        {
            Debug.Log("Bumped my head");
            currentSpeed *= -hitDampeningFactor;
        }
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
