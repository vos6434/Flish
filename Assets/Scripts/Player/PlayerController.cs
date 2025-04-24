using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private string yAxisInput = "Vertical";
    [SerializeField] private string xAxisInput = "Horizontal";
    [SerializeField] private string inputMouseX = "Mouse X";
    [SerializeField] private string inputMouseY = "Mouse Y";
    [SerializeField] private string jumpButton = "Jump";
    [SerializeField] private string fireButton = "Fire1";
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float groundAcceleration = 100f;
    [SerializeField] private float airAcceleration = 100f;
    [SerializeField] private float groundLimit = 12f;
    [SerializeField] private float airLimit = 1f;
    [SerializeField] private float gravity = 16f;
    [SerializeField] private float friction = 6f;
    [SerializeField] private float jumpHeight = 6f;
    [SerializeField] private float rampSlideLimit = 5f;
    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private bool additiveJump = true;
    [SerializeField] private bool autoJump = true;
    [SerializeField] private bool clampGroundSpeed = false;
    [SerializeField] private bool disableBunnyHopping = false;

    [SerializeField] private float jetpackMaxFuel = 100f;
    [SerializeField] private float jetpackFuelConsumption = 25f;

    [SerializeField] private GameObject _camera;

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 10f;

    [Space]
    public Volume wallRunVolume;

    public bool isGravity = false;

    public float wallMaxDistance = 1f;
    public float wallSpeedMultiplier = 1.2f;
    public float minimumHeight = 1.2f;
    public float maxAngleRoll = 20;
    [Range(0.0f, 1.0f)]
    public float normalizedAngleThreshold = 0.1f;
    public float jumpDuration = 1;
    public float wallBouncing = 3;
    public float cameraTransitionDuration = 1;
    public float wallGravityDownForce = 20f;
    Vector3[] directions;
    RaycastHit[] hits;
    bool isWallRunning = false;
    Vector3 lastWallPosition;
    Vector3 lastWallNormal;
    float elapsedTimeSinceJump = 0;
    float elapsedTimeSinceWallAttach = 0;
    float elapsedTimeSinceWallDetach = 0;
    bool jumping;
    float lastVolumeValue = 0;

    private Rigidbody rb;

    private Vector3 vel;
    private Vector3 inputDir;
    private Vector3 _inputRot;
    private Vector3 groundNormal;

    private bool onGround = false;
    private bool jumpPending = false;
    private bool ableToJump = true;

    private bool jumpPressedOnce = false;
    private float doubleTapTime = 0.5f;
    private float lastJumpPressTime = 0f;

    private bool isJetpacking = false;

    private float jetpackFuel = 100f;
    public float JetpackFuel => jetpackFuel;

    public Vector3 InputRot { get => _inputRot; }

    bool CanWallRun()
    {
        float verticalAxis = Input.GetAxis("Vertical");
        return !onGround && verticalAxis > 0 && VerticalCheck();
    }

    bool VerticalCheck()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumHeight);
    }

    void Start() {
        rb = GetComponent<Rigidbody>();

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        directions = new Vector3[]
        {
            Vector3.right,
            Vector3.right + Vector3.forward,
            Vector3.forward,
            Vector3.left + Vector3.forward,
            Vector3.left
        };
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            _camera.SetActive(false);
        }
        else
        {
            _camera.SetActive(true);
            //Debug.Log("Disable Camera");
        }
    }

    private void Update() {
        if (!IsOwner || !IsSpawned) return;
        MouseLook();
        GetMovementInput();

        //Debug.Log(isGravity);
    }


    private void FixedUpdate() {
        vel = rb.linearVelocity; 

        // Clamp speed if bunnyhopping is disabled
        if (disableBunnyHopping && onGround) {
            if (vel.magnitude > groundLimit)
                vel = vel.normalized * groundLimit;
        }

        // Jump
        if (jumpPending && onGround) {
            Jump();
        }

        if (isJetpacking && Input.GetButton(jumpButton))
        {
            Debug.Log("Jetpack pressed");
            ApplyJetpackForce();
        }

        if (!isJetpacking && jetpackFuel < jetpackMaxFuel && onGround)
        {
            jetpackFuel += 0.5f;
            jetpackFuel = Mathf.Clamp(jetpackFuel, 0, jetpackMaxFuel);
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
            if (!isWallRunning)
            {
                ApplyGravity();
            }
        }

        rb.linearVelocity = vel;

        // Reset onGround before next collision checks
        onGround = false;
        groundNormal = Vector3.zero;
    }

    public void LateUpdate()
    {
        isWallRunning = false;

        if (CanAttach())
        {
            hits = new RaycastHit[directions.Length];

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 dir = transform.TransformDirection(directions[i]);
                Physics.Raycast(transform.position, dir, out hits[i], wallMaxDistance);
                if (hits[i].collider != null)
                {
                    Debug.DrawRay(transform.position, dir * hits[i].distance, Color.green);
                }
                else
                {
                    Debug.DrawRay(transform.position, dir * wallMaxDistance, Color.red);
                }
            }
        }

        if (CanWallRun())
        {
            hits = hits.ToList().Where(h => h.collider !=null).OrderBy(h => h.distance).ToArray();
            if (hits.Length > 0)
            {
                OnWall(hits[0]);
                lastWallPosition = hits[0].point;
                lastWallNormal = hits[0].normal;
                //Debug.Log("Calculating wall running");
            }
            else
            {
                isWallRunning = false;
            }
        }

        if (isWallRunning)
        {
            elapsedTimeSinceWallDetach = 0;
            elapsedTimeSinceWallAttach += Time.deltaTime;
            vel = new Vector3(vel.x, vel.y * wallGravityDownForce * Time.deltaTime, vel.z);
        }
        else
        {
            elapsedTimeSinceWallAttach = 0;
            elapsedTimeSinceWallDetach += Time.deltaTime;
        }

        if (!CanWallRun() || hits.Length == 0)
        {
            isWallRunning = false;
            if (lastWallNormal != Vector3.zero && elapsedTimeSinceWallDetach > 0f)
            {
                //Debug.Log("RESETTING WALLRUN VELOCITY");
                //vel -= Vector3.Project(vel, lastWallNormal);
            }
        }

    }

    public Vector3 GetWallJumpDirection()
    {
        if (isWallRunning)
        {
            return lastWallNormal * wallBouncing + Vector3.up;
        }
        return Vector3.zero;
    }
    float CalculateSide()
    {
        if (isWallRunning)
        {
            Vector3 heading = lastWallPosition - transform.position;
            Vector3 perp = Vector3.Cross(transform.forward, heading);
            float dir = Vector3.Dot(perp, transform.up);
            return dir;
        }
        return 0;
    }
    

    bool CanAttach()
    {
        if (!jumpPending)
        {
            elapsedTimeSinceJump += Time.deltaTime;
            if (elapsedTimeSinceJump > jumpDuration)
            {
                elapsedTimeSinceJump = 0;
                jumping = false;
                // /Debug.Log("Can attach to wall");
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

                        //vel = alongWall * vel.y * wallSpeedMultiplier;

                        //add velocity to player
                        //Debug.Log("Wall running");
                        //vel = new Vector3(vel.x, 0, vel.z); // reset players vertical velocity
                        //rb.AddForce(alongWall * vel.y * 1, ForceMode.VelocityChange);
                        //vel = alongWall * 25;
                        //vel += pushTowardsWall; // add force towards wall
                        //vel.y += 2; // reset players vertical velocity

                        //rb.linearVelocity = vel;
                        //vel = new Vector3(vel.x, vel.y*10, vel.z); // reset players vertical velocity
                        //vel = new Vector3(vel.x, 0, vel.z);
                        //rb.AddForce(Vector3.up * 0.065f , ForceMode.VelocityChange);
                        //rb.AddForce(pushTowardsWall, ForceMode.VelocityChange);
                        //rb.AddForce(alongWall * vel.y * 10f, ForceMode.VelocityChange);

                        rb.linearVelocity = new Vector3(vel.x, 0, vel.z); // reset players vertical velocity
                        rb.AddForce(Vector3.up * 0.1f);
                        rb.AddForce(alongWall * 25, ForceMode.Acceleration);
                        Debug.Log("Accelerating Player");
                        isGravity = false;
            

                        isWallRunning = true;
                    }
        }
    }

    void GetMovementInput() {
        float x = Input.GetAxisRaw(xAxisInput);
        float z = Input.GetAxisRaw(yAxisInput);

        inputDir = transform.rotation * new Vector3(x, 0f, z).normalized;

        if (Input.GetButtonDown(jumpButton))
            //jumpPending = true;
            {
                if (jumpPressedOnce && (Time.time - lastJumpPressTime) <= doubleTapTime && jetpackFuel > 0f)
                {
                    Debug.Log("pressed twice");
                    //StartJetpack();
                    jumpPressedOnce = false;
                    isJetpacking = true;
                }
                else
                {
                    jumpPressedOnce = true;
                    lastJumpPressTime = Time.time;
                    jumpPending = true;
                }

            }

        if (Input.GetButtonUp(jumpButton))
            jumpPending = false;
            //StopJetpack();
            /*
            if (!onGround)
            {
                StopJetpack();
            }
            */
            if (isJetpacking && !Input.GetButton(jumpButton))
            {
                StopJetpack();
                //isJetpacking = false;
            }

        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire pressed");
            FireProjectileServerRpc();
        }
    }

    void MouseLook() {
        _inputRot.y += Input.GetAxisRaw(inputMouseX) * mouseSensitivity;
        _inputRot.x -= Input.GetAxisRaw(inputMouseY) * mouseSensitivity;

        if (_inputRot.x > 90f)
            _inputRot.x = 90f;
        if (_inputRot.x < -90f)
            _inputRot.x = -90f;

        transform.rotation = Quaternion.Euler(0f, _inputRot.y, 0f);
    }

    private void GroundAccelerate() {
        float addSpeed = groundLimit - Vector3.Dot(vel, inputDir);

        if (addSpeed <= 0)
            return;

        float accelSpeed = groundAcceleration * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        vel += accelSpeed * inputDir;

        if (clampGroundSpeed) {
            if (vel.magnitude > groundLimit)
                vel = vel.normalized * groundLimit;
        }
    }

    private void AirAccelerate() {
        Vector3 hVel = vel;
        hVel.y = 0;

        float addSpeed =  airLimit - Vector3.Dot(hVel, inputDir);

        if (addSpeed <= 0)
            return;

        float accelSpeed = airAcceleration * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        vel += accelSpeed * inputDir;
    }

    private void ApplyFriction() {
        vel *= Mathf.Clamp01(1 - Time.deltaTime * friction);
    }

    private void Jump() {
        if (!ableToJump)
            return;

        if (vel.y < 0f || !additiveJump)
            vel.y = 0f;

        vel.y += jumpHeight;
        onGround = false;

        if (!autoJump)
            jumpPending = false;

        StartCoroutine(JumpTimer());
    }

    

    private void ApplyGravity() {
        vel.y -= gravity * Time.deltaTime;
        isGravity = true;
    }

    private void OnCollisionStay(Collision other) {
        // Check if any of the contacts has acceptable floor angle
        foreach (ContactPoint contact in other.contacts) {
            if (contact.normal.y > Mathf.Sin(slopeLimit * (Mathf.PI / 180f) + Mathf.PI/2f)) {
                groundNormal = contact.normal;
                onGround = true;
                return;
            }
        }
    }

    private void StartJetpack()
    {
        if (!isJetpacking)
        {
            isJetpacking = true;
            Debug.Log("Start Jetpack");
        }
    }

    
    private void StopJetpack()
    {
        if (isJetpacking)
        {
            isJetpacking = false;
            Debug.Log("Disable Jetpack");
        }
    }

    private void ApplyJetpackForce()
    {
        Vector3 forwardDirection = _camera.transform.TransformDirection(Vector3.forward);

        if (jetpackFuel > 0f)
        {
            Debug.Log("add force");
            jetpackFuel -= jetpackFuelConsumption * Time.deltaTime;
            vel = new Vector3(vel.x, 0, vel.z); // reset players vertical velocity

            rb.AddForce(Vector3.up * 10 , ForceMode.VelocityChange);
            rb.AddForce(forwardDirection * 0.5f, ForceMode.VelocityChange);
        }
        else if (jetpackFuel < 0f)
        { 
            jetpackFuel = 0f;
        }
    }

    // This is for avoiding multiple consecutive jump commands before leaving ground
    private IEnumerator JumpTimer() {
        ableToJump = false;
        yield return new WaitForSeconds(0.1f);
        ableToJump = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void FireProjectileServerRpc()
    {
        if (projectilePrefab != null)
        {
            Vector3 spawnPosition = _camera.transform.position + _camera.transform.forward * 0.7f; // Adjust spawn position as needed
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, _camera.transform.rotation);
            //projectile.GetComponent<bullet>().parent = gameObject; // Set the parent of the bullet to the player

            //projectile.transform.SetParent(transform);
            projectile.GetComponent<bullet>().Owner = gameObject;

            projectile.GetComponent<NetworkObject>().Spawn(); // Spawn the projectile on the network

            Rigidbody projectilerb = projectile.GetComponent<Rigidbody>();
            if (projectilerb != null)
            {
                projectilerb.AddForce(_camera.transform.forward * projectileSpeed, ForceMode.VelocityChange); // Add force to the projectile
                //projectilerb.linearVelocity = _camera.transform.forward * projectileSpeed; // Set the projectile's velocity directly
            }
        }
    }
}