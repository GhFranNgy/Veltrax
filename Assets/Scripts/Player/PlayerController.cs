using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;      // Camera parent
    public Transform gunHolder;          // Optional gun holder (child)

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.5f;
    public float maxLookAngle = 85f;

    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float proneSpeed = 1.2f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.6f;
    public float gravity = -20f;

    [Header("Character Heights")]
    public float standingHeight = 1.8f;
    public float crouchHeight = 1.2f;
    public float proneHeight = 0.6f;

    [Header("View Bobbing")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode proneKey = KeyCode.Z;

    // State
    public bool isRunning;
    public bool isCrouching;
    public bool isProne;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation;
    private float bobTimer;
    private Vector3 cameraStartLocalPos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraStartLocalPos = cameraHolder.localPosition;
        controller.height = standingHeight;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MouseLook();
        Movement();
        HandleStance();
        ViewBob();
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        isRunning = Input.GetKey(runKey) && !isCrouching && !isProne;

        float speed = walkSpeed;
        if (isRunning) speed = runSpeed;
        if (isCrouching) speed = crouchSpeed;
        if (isProne) speed = proneSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            if (Input.GetKeyDown(jumpKey) && !isCrouching && !isProne)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleStance()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = !isCrouching;
            isProne = false;
        }

        if (Input.GetKeyDown(proneKey))
        {
            isProne = !isProne;
            isCrouching = false;
        }

        float targetHeight = standingHeight;
        if (isCrouching) targetHeight = crouchHeight;
        if (isProne) targetHeight = proneHeight;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);
    }

    void ViewBob()
    {
        if (!controller.isGrounded) return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
            cameraHolder.localPosition = cameraStartLocalPos + Vector3.up * bobOffset;
        }
        else
        {
            bobTimer = 0;
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                cameraStartLocalPos,
                Time.deltaTime * 10f
            );
        }
    }
}
