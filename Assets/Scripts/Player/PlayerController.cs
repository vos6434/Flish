using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private string yAxisInput = "Vertical"; // Vertical input axis
    [SerializeField] private string xAxisInput = "Horizontal"; // Horizontal input axis
    [SerializeField] private string inputMouseX = "Mouse X"; // Mouse X input axis
    [SerializeField] private string inputMouseY = "Mouse Y"; // Mouse Y input axis
    [SerializeField] private string jumpButton = "Jump"; // Jump button input
    [SerializeField] private string fireButton = "Fire1"; // Shoot button input
    [SerializeField] private float mouseSensitivity = 1f; // Mouse sensitivity
    [SerializeField] private float groundAcceleration = 100f; // Ground acceleration speed
    [SerializeField] private float airAcceleration = 100f; // Air acceleration speed
    [SerializeField] private float groundLimit = 12f; // Maximum speed on ground
    [SerializeField] private float airLimit = 1f; // Maximum speed in air
    [SerializeField] private float gravity = 16f; // Gravity force
    [SerializeField] private float friction = 6f; // Friction force
    [SerializeField] private float jumpHeight = 6f; // Jump height
    [SerializeField] private float rampSlideLimit = 5f; // Ramp slide limit
    [SerializeField] private float slopeLimit = 45f; // Maximum slope angle
    [SerializeField] private bool additiveJump = true;
    [SerializeField] private bool autoJump = true;
    [SerializeField] private bool clampGroundSpeed = false; // Clamp speed on ground
    [SerializeField] private bool disableBunnyHopping = false;

    [SerializeField] private float jetpackMaxFuel = 100f; // Maximum jetpack fuel
    [SerializeField] private float jetpackFuelConsumption = 25f; // Jetpack fuel consumption rate

    [SerializeField] private GameObject _camera; // The camera object

    [SerializeField] private GameObject projectilePrefab; // The projectile prefab to be spawned
    [SerializeField] private Transform projectileSpawnPoint; // The spawn point location for the projectile
    [SerializeField] private float projectileSpeed = 10f; // The speed of the projectile

    public bool isGravity = false; // Is gravity enabled
    public float wallMaxDistance = 0.5f; // Maximum distance for wall running detection
    public float minimumHeight = 1.2f; // Minimum height for wall running
    public float normalizedAngleThreshold = 0.1f; // Angle threshold for wall running
    public float jumpDuration = 1; // Duration of the jump
    public float wallBouncing = 3; // Wall bouncing force
    public float wallGravityDownForce = 20f; // Gravity force when wall running
    Vector3[] directions; // Directions for wall running detection
    RaycastHit[] hits; // Array of raycast hits for wall running detection
    bool isWallRunning = false; // is player wall running
    Vector3 lastWallPosition; // Last wall position for wall running
    Vector3 lastWallNormal; // Last wall normal for wall running
    float elapsedTimeSinceJump = 0; // Elapsed time since jump
    float elapsedTimeSinceWallAttach = 0; // Elapsed time since wall running attach
    float elapsedTimeSinceWallDetach = 0; // Elapsed time since wall running detach
    bool jumping; // Is player jumping

    private Rigidbody rb; // Player rigidbody

    private Vector3 vel; // Player velocity
    private Vector3 inputDir; // Player input direction
    private Vector3 _inputRot; // Player input rotation
    private Vector3 groundNormal; // Ground normal for player movement

    private bool onGround = false; // Is player on ground
    private bool jumpPending = false; // Is player jump pending
    private bool ableToJump = true; // Is player able to jump

    private bool jumpPressedOnce = false; // Has player jump pressed once
    private float doubleTapTime = 0.5f; // Time limit for double jump press
    private float lastJumpPressTime = 0f; // Last jump press time

    private bool isJetpacking = false; // Is player using jetpack

    private float jetpackFuel = 100f; // Current jetpack fuel
    public float JetpackFuel => jetpackFuel; // pointer to read only jetpack fuel

    public Vector3 InputRot { get => _inputRot; } // pointer to read only input rotation

    bool CanWallRun() // Check if the player can wall run
    {
        float verticalAxis = Input.GetAxis("Vertical"); // Get vertical input axis
        return !onGround && verticalAxis > 0 && VerticalCheck(); // Check if player is not on ground and vertical check
    }

    bool VerticalCheck()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumHeight); // Check if player is above minimum height
    }

    void Start() {
        rb = GetComponent<Rigidbody>(); // Get the rigidbody component

        Cursor.visible = false; // Hide mouse cursor
        Cursor.lockState = CursorLockMode.Locked; // Lock mouse cursor

        directions = new Vector3[] // Define directions for wall running detection
        {
            Vector3.right,
            Vector3.right + Vector3.forward,
            Vector3.forward,
            Vector3.left + Vector3.forward,
            Vector3.left
        };
    }

    public override void OnNetworkSpawn() // When the object is spawned on server
    {
        if (!IsOwner) // Disable if the object is not owned by the player
        {
            _camera.SetActive(false);
        }
        else
        {
            _camera.SetActive(true); // Enable if the object is owned by the player
        }
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned) return; // If not owned or not spawned, return
        MouseLook();
        GetMovementInput();
    }


    private void FixedUpdate()
    {
        vel = rb.linearVelocity; // Get the current velocity

        // Clamp speed if bunnyhopping is disabled
        if (disableBunnyHopping && onGround) {
            if (vel.magnitude > groundLimit)
                vel = vel.normalized * groundLimit;
        }

        // Jump
        if (jumpPending && onGround) // If jump is pending and player is on ground
        {
            Jump();
        }

        if (isJetpacking && Input.GetButton(jumpButton)) // If jetpack is active and jump button is held
        {
            //Debug.Log("Jetpack pressed");
            ApplyJetpackForce();
        }

        if (!isJetpacking && jetpackFuel < jetpackMaxFuel && onGround) // If not jetpacking and jetpack fuel is less than max fuel and player is on ground
        {
            jetpackFuel += 0.5f; // Regenerate jetpack fuel
            jetpackFuel = Mathf.Clamp(jetpackFuel, 0, jetpackMaxFuel); // Clamp jetpack fuel to max value
        }

        // We use air physics if moving upwards at high speed
        if (rampSlideLimit >= 0f && vel.y > rampSlideLimit)
            onGround = false;

        if (onGround) {
            // Rotate movement vector to match ground tangent
            inputDir = Vector3.Cross(Vector3.Cross(groundNormal, inputDir), groundNormal);

            GroundAccelerate();
            ApplyFriction();
        }
        else {
            AirAccelerate();
            if (!isWallRunning) // If not wall running, apply gravity
            {
                ApplyGravity();
            }
        }

        rb.linearVelocity = vel; // Set the new velocity

        // Reset onGround before next collision checks
        onGround = false;
        groundNormal = Vector3.zero;
    }

    public void LateUpdate()
    {
        isWallRunning = false; // Set wall running to false

        if (CanAttach()) // Check if player can attach to wall
        {
            hits = new RaycastHit[directions.Length]; // Initialize hits array

            for (int i = 0; i < directions.Length; i++) // Loop through all directions for wall detection
            {
                Vector3 dir = transform.TransformDirection(directions[i]); // Convert direction to world space
                Physics.Raycast(transform.position, dir, out hits[i], wallMaxDistance); // Perform a raycast in the specified direction up to the maximum wall detection distance
                if (hits[i].collider != null) // If raycast hit a collider
                {
                    Debug.DrawRay(transform.position, dir * hits[i].distance, Color.green); // Draw a green ray if hit within the hit distance
                }
                else
                {
                    Debug.DrawRay(transform.position, dir * wallMaxDistance, Color.red); // Draw a red ray if max distance is reached without hitting a collider
                }
            }
        }

        if (CanWallRun()) // Check if the player can wall run
        {
            hits = hits.ToList() // Convert hits array to a list
                .Where(h => h.collider !=null) // Filter out hits that did not collide
                .OrderBy(h => h.distance) // Order hits by distance
                .ToArray(); // Convert back to array

            if (hits.Length > 0) // Check if there are valid hits
            {
                OnWall(hits[0]); // OnWall on the closest hit
                lastWallPosition = hits[0].point; // Remember the closest wall hit
                lastWallNormal = hits[0].normal; // Remember the normal of the closest wall hit
            }

            else
            {
                isWallRunning = false; // If no hits, set wall running to false
            }
        }

        if (isWallRunning) // If player is wall running
        {
            elapsedTimeSinceWallDetach = 0; // Set the elapsed time since wall detach to 0
            elapsedTimeSinceWallAttach += Time.deltaTime; // Track the time since wall attach
            vel = new Vector3(vel.x, vel.y * wallGravityDownForce * Time.deltaTime, vel.z); // Apply wall running gravity
        }
        else // If player is not wall running
        {
            elapsedTimeSinceWallAttach = 0; // Reset the time since wall attach
            elapsedTimeSinceWallDetach += Time.deltaTime; // Track the time since wall detach
        }

        if (!CanWallRun() || hits.Length == 0) // If the player cannot wall run or there are no valid hits
        {
            isWallRunning = false; // Set wall running to false
            if (lastWallNormal != Vector3.zero && elapsedTimeSinceWallDetach > 0f) // Check if there is a valid last wall normal andif time has passed since last wall detatch
            {
                //Debug.Log("resettubg wakk run velocity");
                //vel -= Vector3.Project(vel, lastWallNormal);
            }
        }

    }

    public Vector3 GetWallJumpDirection() // Calculate the direction for a wall jump
    {
        if (isWallRunning) // If player is wall running
        {
            return lastWallNormal * wallBouncing + Vector3.up; // Calculate the wall jump direction based on the last wall normal and add an upward force
        }
        return Vector3.zero; // If the player is not wall running, return a zero vector
    }

    bool CanAttach() // Check if the player can attach to a wall
    {
        if (!jumpPending) // Check if player is not pending a jump
        {
            elapsedTimeSinceJump += Time.deltaTime; // Track the time since last jump
            if (elapsedTimeSinceJump > jumpDuration) // If the time since last jump exceeds the jump duration
            {
                elapsedTimeSinceJump = 0; // Reset the alapsed time since jump
                jumping = false; // Player is no longer juping
            }
            return false;
        }
        return true;
    }

    void OnWall(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Wallrun"))
        {

            // Check if the wall normal is within the acceptable angle range
            float d = Vector3.Dot(hit.normal, Vector3.up);
            if (d >= -normalizedAngleThreshold && d <= normalizedAngleThreshold)
            {

                float verticalAxis = Input.GetAxis("Vertical");
                Vector3 alongWall = transform.TransformDirection(Vector3.forward);

                Debug.DrawRay(transform.position, alongWall.normalized * 10, Color.green);
                Debug.DrawRay(transform.position, lastWallNormal * 10, Color.magenta);

                Vector3 pushTowardsWall = hit.normal * -5f;

                rb.linearVelocity = new Vector3(vel.x, 0, vel.z); // reset players vertical velocity
                rb.AddForce(Vector3.up * 0.1f);
                rb.AddForce(alongWall * 25, ForceMode.Acceleration);
                //Debug.Log("Accelerating Player");
                isGravity = false;

                isWallRunning = true;
            }
        }
    }

    void GetMovementInput() // Get player movement input
    {
        float x = Input.GetAxisRaw(xAxisInput); // Get horizontal input axis
        float z = Input.GetAxisRaw(yAxisInput); // Get vertical input axis

        inputDir = transform.rotation * new Vector3(x, 0f, z).normalized; // Calculate the input direction based on the player's rotation

        if (Input.GetButtonDown(jumpButton)) // Check if jump button was pressed
            //jumpPending = true;
            {
                if (jumpPressedOnce && (Time.time - lastJumpPressTime) <= doubleTapTime && jetpackFuel > 0f) // If jump button was pressed twice and if there is enough fuel
                {
                    //Debug.Log("pressed twice");
                    //StartJetpack();
                    jumpPressedOnce = false; // Disable jump pressed once
                    isJetpacking = true; // Start jetpack
                }
                else // Double jump not pressed
                {
                    jumpPressedOnce = true; // Enable jump pressed once
                    lastJumpPressTime = Time.time; // Time since last jump press
                    jumpPending = true; // Set jump pending to true
                }

            }

        if (Input.GetButtonUp(jumpButton)) // If jump button key is released or not pressed
            jumpPending = false; // Set jump pending to false
            if (isJetpacking && !Input.GetButton(jumpButton)) // If player is jetpacking and jump button is not pressed
            {
                StopJetpack(); // Stop the jetpack
            }

        if (Input.GetButtonDown("Fire1")) // Check if the shooting button is pressed
        {
            FireProjectileServerRpc(); // Call the server RPC to fire a projectile
        }
    }

    void MouseLook() // Handle mouse input for player rotation
    {
        _inputRot.y += Input.GetAxisRaw(inputMouseX) * mouseSensitivity; // Adjust player horizontal rotation based on mouse x axis and sensitivity
        _inputRot.x -= Input.GetAxisRaw(inputMouseY) * mouseSensitivity; // Adjust player vertical rotation based on mouse y axis and sensitivity

        if (_inputRot.x > 90f) // Prevent the player from looking too far up
            _inputRot.x = 90f;

        if (_inputRot.x < -90f) // Prevent the player from looking too far down
            _inputRot.x = -90f;

        transform.rotation = Quaternion.Euler(0f, _inputRot.y, 0f); // Rotate the player based on the horizontal input rotation
    }

    private void GroundAccelerate() // Handle ground acceleration
    {
        float addSpeed = groundLimit - Vector3.Dot(vel, inputDir); // Calculate the additional speed needed to reach the ground limit

        if (addSpeed <= 0) // If no additional speed is needed, return
            return;

        float accelSpeed = groundAcceleration * Time.deltaTime; // Calculate the acceleration speed

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        vel += accelSpeed * inputDir; // Apply the acceleration to the player velocity

        if (clampGroundSpeed)
        {
            if (vel.magnitude > groundLimit)
                vel = vel.normalized * groundLimit;
        }
    }

    private void AirAccelerate() // Handle air acceleration
    {
        Vector3 hVel = vel; // Get player horizontal velocity
        hVel.y = 0;

        float addSpeed =  airLimit - Vector3.Dot(hVel, inputDir); // Calculate the additional speed needed to reach the air limit

        if (addSpeed <= 0) // If no additional speed is needed, return
            return;

        float accelSpeed = airAcceleration * Time.deltaTime; // Calculate the acceleration speed

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        vel += accelSpeed * inputDir;
    }

    private void ApplyFriction() // Apply friction to player velocity
    {
        vel *= Mathf.Clamp01(1 - Time.deltaTime * friction); // Apply friction to the player velocity with time
    }

    private void Jump() // Handle player jumping
    {
        if (!ableToJump) // If player is not able to jump, return
            return;

        if (vel.y < 0f || !additiveJump) // If player is falling or additive jump is disabled then reset vertical velocity
            vel.y = 0f;

        vel.y += jumpHeight; // Add player jump height to vertical velocity
        onGround = false; // Player is not on ground

        if (!autoJump) // If auto jump is disabled
            jumpPending = false; // Set jump pending to false

        StartCoroutine(JumpTimer()); // Delay the player from jumping again too quickly
    }

    

    private void ApplyGravity() // Handle gravity for the player
    {
        vel.y -= gravity * Time.deltaTime; // Apply gravity to the player velocity
        isGravity = true; // Set gravity to true
    }

    private void OnCollisionStay(Collision other) // If player is colliding with something
    {
        // Check if any of the contacts has acceptable floor angle
        foreach (ContactPoint contact in other.contacts) {
            if (contact.normal.y > Mathf.Sin(slopeLimit * (Mathf.PI / 180f) + Mathf.PI/2f)) {
                groundNormal = contact.normal;
                onGround = true;
                return;
            }
        }
    }

    private void StartJetpack() // Start the jetpack
    {
        if (!isJetpacking)
        {
            isJetpacking = true;
            //Debug.Log("Start Jetpack");
        }
    }

    
    private void StopJetpack() // Stop the jetpack
    {
        if (isJetpacking)
        {
            isJetpacking = false;
            //Debug.Log("Disable Jetpack");
        }
    }

    private void ApplyJetpackForce() // Apply force to the player when using the jetpack
    {
        Vector3 forwardDirection = _camera.transform.TransformDirection(Vector3.forward); // Get the forward direction of the camera

        if (jetpackFuel > 0f) // If the jetpack has fuel
        {
            //Debug.Log("add force");
            jetpackFuel -= jetpackFuelConsumption * Time.deltaTime; // Consume jetpack fuel

            vel = new Vector3(vel.x, 0, vel.z); // reset players vertical velocity
            rb.AddForce(Vector3.up * 10 , ForceMode.VelocityChange); // Add upward force to the player
            rb.AddForce(forwardDirection * 0.5f, ForceMode.VelocityChange); // Add forward force to the player
        }
        else if (jetpackFuel < 0f) // If jetpack fuel is less than 0
        { 
            jetpackFuel = 0f; // Set jetpack fuel to 0
        }
    }

    // This is for avoiding multiple consecutive jump commands before leaving ground
    private IEnumerator JumpTimer() {
        ableToJump = false;
        yield return new WaitForSeconds(0.1f);
        ableToJump = true;
    }

    [ServerRpc(RequireOwnership = false)] // Server RPC to fire a projectile
    private void FireProjectileServerRpc()
    {
        if (projectilePrefab != null) // Check if projetile prefab is assigned
        {
            Vector3 spawnPosition = _camera.transform.position + _camera.transform.forward * 0.7f; // Calculate the spawn position for the projectile prefab
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, _camera.transform.rotation); // Instantiate the projectile prefab at the spawn position and rotation

            projectile.GetComponent<bullet>().Owner = gameObject; // Set the owner of the projectile as the player
            projectile.GetComponent<NetworkObject>().Spawn(); // Spawn the projectile on the server

            Rigidbody projectilerb = projectile.GetComponent<Rigidbody>(); // Get the projectile rigidbody
            if (projectilerb != null)
            {
                projectilerb.AddForce(_camera.transform.forward * projectileSpeed, ForceMode.VelocityChange); // Add force to the projectile
                //projectilerb.linearVelocity = _camera.transform.forward * projectileSpeed; // Set the projectile velocity
            }
        }
    }
}