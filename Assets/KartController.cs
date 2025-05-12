using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class KartController : MonoBehaviour
{
    public float maxSpeed = 100f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    public float turnSpeed = 80f;
    public float fallingSpeed = 120f;

    public float raycastDistance = 1f; // Distance to check for the track surface
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

    void Start()
    {
        // Get the BoxCollider component
        boxCollider = GetComponent<BoxCollider>();

        //get the start loc and rot
        respawnPosition = transform.position;
        respawnRotation = transform.rotation;
}

    void Update()
    {
        //respawn if falls through world
        if (transform.position.y < -20)
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
                currentSpeed -= deceleration/2 * Time.deltaTime;
            }
            else if (currentSpeed < 0)
            {
                currentSpeed += deceleration/2 * Time.deltaTime;
            }
        }

        // Clamp the speed to the max speed
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // Forward, backward and steering
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, turnDirection * turnSpeed * Time.deltaTime);

        // Raycast to detect the track surface below the kart for position
        RaycastHit positionHit;
        if (Physics.Raycast(transform.position, Vector3.down, out positionHit, raycastDistance, trackLayer))
        {
            // Calculate the angle difference between the current up direction and the track normal
            float angleDifference = Vector3.Angle(transform.up, positionHit.normal);

            // Adjust the rotation speed based on the angle difference
            float adjustedGimbalSpeed = gimbalSpeed * (angleDifference / 90f); // Scale speed based on angle difference

            // Align the kart's rotation with the track's normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, positionHit.normal) * transform.rotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * adjustedGimbalSpeed);

            // Adjust the kart's position to stick to the track
            transform.position = positionHit.point + Vector3.up * (boxCollider.bounds.size.y / 2);
        }
        else
        {
            transform.position -= Vector3.down * fallingSpeed * Time.deltaTime;
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


    void respawn()
    {
        // Respawn the kart at the specified position and rotation
        transform.position = respawnPosition;
        transform.rotation = respawnRotation;
        currentSpeed = 0f; // Reset speed
    }
}
