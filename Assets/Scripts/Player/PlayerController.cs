using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
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

    [SerializeField] private float jetpackMaxFuel = 100f;
    [SerializeField] private float jetpackFuelConsumption = 25f;

    [SerializeField] private GameObject _camera;

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

    void Start() {
        rb = GetComponent<Rigidbody>();

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
}