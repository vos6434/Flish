using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    public LayerMask wallLayer;
    [SerializeField] private string yAxisInput = "Vertical";
    [SerializeField] private string xAxisInput = "Horizontal";
    [SerializeField] private string inputMouseX = "Mouse X";
    [SerializeField] private string inputMouseY = "Mouse Y";
    [SerializeField] private string jumpButton = "Jump";
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
    [SerializeField] private GameObject _camera;

    private Rigidbody rb;

    private Vector3 vel;
    private Vector3 inputDir;
    private Vector3 _inputRot;
    private Vector3 groundNormal;
    private Vector3 wallNormal;

    private Vector3[] directions;
    private RaycastHit[] hits;

    private bool onGround = false;
    private bool jumpPending = false;
    private bool ableToJump = true;

    private bool isWallRunning = false;

    public Vector3 InputRot { get => _inputRot; }

    void Start() {
        rb = GetComponent<Rigidbody>();

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        directions = new Vector3[]
        {
            Vector3.right,
            Vector3.right + Vector3.forward,
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
        CanWallRun();
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

        // We use air physics if moving upwards at high speed
        if (rampSlideLimit >= 0f && vel.y > rampSlideLimit)
            onGround = false;

        if (isWallRunning)
        {
            rb.velocity = vel;
            return;
        }

        if (onGround) {
            // Rotate movement vector to match ground tangent
            inputDir = Vector3.Cross(Vector3.Cross(groundNormal, inputDir), groundNormal);

            GroundAccelerate();
            ApplyFriction();
        }
        else {
            ApplyGravity();
            AirAccelerate();
        }

        rb.linearVelocity = vel;

        // Reset onGround before next collision checks
        onGround = false;
        groundNormal = Vector3.zero;
    }

    void GetMovementInput() {
        float x = Input.GetAxisRaw(xAxisInput);
        float z = Input.GetAxisRaw(yAxisInput);

        inputDir = transform.rotation * new Vector3(x, 0f, z).normalized;

        if (Input.GetButtonDown(jumpButton))
            jumpPending = true;

        if (Input.GetButtonUp(jumpButton))
            jumpPending = false;
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

    private void CanWallRun()
    {
        hits = new RaycastHit[directions.Length];
        isWallRunning = false;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 dir = transform.TransformDirection(directions[i]);
            Physics.Raycast(transform.position, dir, out hits[i], 1f, wallLayer);

            if (hits[i].collider != null)
            {
                Debug.DrawRay(transform.position, dir * hits[i].distance, Color.green);

                if (Vector3.Dot(hits[i].normal, Vector3.up) < 0.1f)
                {
                    isWallRunning = true;
                    wallNormal = hits[i].normal;
                    break;
                }
            }
            else
            {
                Debug.DrawRay(transform.position, dir * 1f, Color.red);
            }
        }

        if (isWallRunning)
        {
            WallRun();
        }
    }

    private void WallRun()
    {
        Debug.Log("WallRun function");

        vel.y = 0f; //cancel gravity

        // adjust velociity to move along the wall
        Vector3 alongWall = Vector3.Cross(wallNormal, Vector3.up).normalized;

        if (Vector3.Dot(alongWall, inputDir) < 0f)
        {
            alongWall = -alongWall; // flip direction if moving away from wall
        }
        vel = alongWall * groundLimit;
        Debug.Log("Zero gravity");


        // allow to jummp off the wall
        if (Input.GetButtonDown(jumpButton) && ableToJump)
        {
            vel += wallNormal * jumpHeight; // push away from the wall
            vel.y = jumpHeight; // add upward force
            //onGround = false;
            //StartCoroutine(JumpTimer());
            isWallRunning = false; // exit wall run
        }

        //rb.AddForce(-wallNormal * 10f, ForceMode.Force); // apply force to keep player on the wall
        rb.velocity = vel;
    }

    // This is for avoiding multiple consecutive jump commands before leaving ground
    private IEnumerator JumpTimer() {
        ableToJump = false;
        yield return new WaitForSeconds(0.1f);
        ableToJump = true;
    }
}