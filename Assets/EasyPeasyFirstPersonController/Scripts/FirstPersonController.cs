namespace EasyPeasyFirstPersonController
{
    using System;
    using UnityEngine;

    public partial class FirstPersonController : MonoBehaviour
    {
        // --- Settings ---
        [Range(0, 100)] public float mouseSensitivity = 50f;
        [Range(0f, 200f)] private float snappiness = 100f;
        [Range(0f, 20f)] public float walkSpeed = 3f;
        [Range(0f, 30f)] public float sprintSpeed = 5f;
        [Range(0f, 10f)] public float crouchSpeed = 1.5f;
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 1f;
        public float slideSpeed = 8f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;
        [Range(0f, 15f)] public float jumpSpeed = 3f;
        [Range(0f, 50f)] public float gravity = 9.81f;

        public bool coyoteTimeEnabled = true;
        [Range(0.01f, 0.3f)] public float coyoteTimeDuration = 0.2f;

        // --- FOV ---
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float fovChangeSpeed = 5f;

        // --- ADS / ZOOM ---
        public KeyCode keyAim = KeyCode.Mouse1;
        public float adsFov = 45f;
        [Range(0.1f, 20f)] public float adsFovSpeed = 10f;

        public float walkingBobbingSpeed = 10f;
        public float bobbingAmount = 0.05f;
        public float sprintBobMultiplier = 1.5f;
        private float recoilReturnSpeed = 8f;

        // --- Feature toggles ---
        public bool canSlide = true;
        public bool canJump = true;
        public bool canSprint = true;
        public bool canCrouch = true;

        // --- Physics & Camera ---
        public QueryTriggerInteraction ceilingCheckQueryTriggerInteraction = QueryTriggerInteraction.Ignore;
        public QueryTriggerInteraction groundCheckQueryTriggerInteraction = QueryTriggerInteraction.Ignore;
        public Transform groundCheck;
        public float groundDistance = 0.2f;
        public LayerMask groundMask;
        public Transform playerCamera;
        public Transform cameraParent;

        // --- Keybinds ---
        public KeyCode keyMoveForward = KeyCode.W;
        public KeyCode keyMoveBackward = KeyCode.S;
        public KeyCode keyMoveLeft = KeyCode.A;
        public KeyCode keyMoveRight = KeyCode.D;
        public KeyCode keyJump = KeyCode.Space;
        public KeyCode keySprint = KeyCode.LeftShift;
        public KeyCode keyCrouch = KeyCode.LeftControl;
        public KeyCode keySlide = KeyCode.LeftControl;

        // --- Internal variables ---
        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private bool isGrounded;
        private Vector2 moveInput;
        public bool isSprinting;
        public bool isCrouching;
        public bool isSliding;
        private float slideTimer;
        private float postSlideCrouchTimer;
        private Vector3 slideDirection;
        private float originalHeight;
        private float originalCameraParentHeight;
        private float coyoteTimer;
        private Camera cam;
        private float bobTimer;
        private Vector3 recoil = Vector3.zero;
        private bool isLook = true, isMove = true;
        private float currentCameraHeight;
        private float currentBobOffset;
        private float currentFov;
        private float fovVelocity;
        private float currentSlideSpeed;
        private float slideSpeedVelocity;
        private float currentTiltAngle;
        private float tiltVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            cam = playerCamera.GetComponent<Camera>();

            originalHeight = characterController.height;
            originalCameraParentHeight = cameraParent.localPosition.y;

            currentCameraHeight = originalCameraParentHeight;
            currentFov = normalFov;

            rotX = transform.rotation.eulerAngles.y;
            rotY = playerCamera.localRotation.eulerAngles.x;
            xVelocity = rotX;
            yVelocity = rotY;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask,
                groundCheckQueryTriggerInteraction
            );

            if (isGrounded && moveDirection.y < 0)
            {
                moveDirection.y = -2f;
                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled)
            {
                coyoteTimer -= Time.deltaTime;
            }

            HandleLook();
            HandleHeadBob();
            HandleCrouchAndSlide();
            HandleMovement();
            HandleFov(); // ADS ZOOM IS HERE
        }

        private void HandleLook()
        {
            if (!isLook) return;

            float sensitivity = mouseSensitivity;

            if (Input.GetKey(keyAim))
                sensitivity *= 0.6f; // optional: ADS sensitivity reduction

            float mouseX = Input.GetAxis("Mouse X") * 10 * sensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * 10 * sensitivity * Time.deltaTime;

            rotX += mouseX;
            rotY -= mouseY;
            rotY = Mathf.Clamp(rotY, -90f, 90f);

            xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
            yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

            float targetTilt = isSliding ? slideTiltAngle : 0f;
            currentTiltAngle = Mathf.SmoothDamp(currentTiltAngle, targetTilt, ref tiltVelocity, 0.2f);

            playerCamera.localRotation = Quaternion.Euler(yVelocity - currentTiltAngle, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
        }

        private void HandleFov()
        {
            float slideProgress = slideTimer / slideDuration;
            bool isAiming = Input.GetKey(keyAim);

            float targetFov = normalFov;

            if (isSliding)
            {
                targetFov = sprintFov + slideFovBoost *
                    Mathf.Lerp(0f, 1f, 1f - slideProgress);
            }
            else if (isAiming)
            {
                targetFov = adsFov;
            }
            else if (isSprinting)
            {
                targetFov = sprintFov;
            }

            float speed = isAiming ? adsFovSpeed : fovChangeSpeed;

            currentFov = Mathf.SmoothDamp(
                currentFov,
                targetFov,
                ref fovVelocity,
                1f / speed
            );

            cam.fieldOfView = currentFov;
        }

        private void HandleMovement()
        {
            moveInput = Vector2.zero;
            if (Input.GetKey(keyMoveForward)) moveInput.y += 1f;
            if (Input.GetKey(keyMoveBackward)) moveInput.y -= 1f;
            if (Input.GetKey(keyMoveRight)) moveInput.x += 1f;
            if (Input.GetKey(keyMoveLeft)) moveInput.x -= 1f;

            isSprinting = canSprint && Input.GetKey(keySprint) &&
                          moveInput.y > 0.1f && isGrounded &&
                          !isCrouching && !isSliding &&
                          !Input.GetKey(keyAim);

            float speed = isCrouching ? crouchSpeed :
                          isSprinting ? sprintSpeed : walkSpeed;

            Vector3 move = transform.TransformDirection(
                new Vector3(moveInput.x, 0f, moveInput.y)
            ) * speed;

            if (isGrounded || coyoteTimer > 0f)
            {
                if (canJump && Input.GetKeyDown(keyJump) && !isSliding)
                    moveDirection.y = jumpSpeed;
            }
            else
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            if (!isSliding)
            {
                moveDirection.x = move.x;
                moveDirection.z = move.z;
                characterController.Move(moveDirection * Time.deltaTime);
            }
        }

        private void HandleCrouchAndSlide()
        {
            bool wantsCrouch = canCrouch && Input.GetKey(keyCrouch) && !isSliding;

            if (canSlide && isSprinting && Input.GetKeyDown(keySlide) && isGrounded)
            {
                isSliding = true;
                slideTimer = slideDuration;
                slideDirection = transform.forward;
                currentSlideSpeed = sprintSpeed;
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f || !isGrounded)
                    isSliding = false;

                float targetSpeed = slideSpeed;
                currentSlideSpeed = Mathf.SmoothDamp(
                    currentSlideSpeed,
                    targetSpeed,
                    ref slideSpeedVelocity,
                    0.2f
                );

                characterController.Move(slideDirection * currentSlideSpeed * Time.deltaTime);
            }

            isCrouching = wantsCrouch || isSliding;

            float targetHeight = isCrouching ? crouchHeight : originalHeight;
            characterController.height = Mathf.Lerp(
                characterController.height,
                targetHeight,
                Time.deltaTime * 10f
            );

            characterController.center = new Vector3(
                0f,
                characterController.height * 0.5f,
                0f
            );
        }

        private void HandleHeadBob()
        {
            Vector3 horizontalVel = new Vector3(
                characterController.velocity.x,
                0f,
                characterController.velocity.z
            );

            bool moving = horizontalVel.magnitude > 0.1f;
            bool aiming = Input.GetKey(keyAim);

            if (!isGrounded || isSliding || isCrouching || aiming)
            {
                bobTimer = 0f;
                cameraParent.localPosition = new Vector3(
                    0f,
                    originalCameraParentHeight,
                    0f
                );
                return;
            }

            if (moving)
            {
                float bobSpeed = walkingBobbingSpeed * (isSprinting ? sprintBobMultiplier : 1f);
                bobTimer += Time.deltaTime * bobSpeed;

                // Vertical bob
                float bobY = Mathf.Sin(bobTimer) * bobbingAmount;
                // Horizontal bob (side-to-side)
                float bobX = Mathf.Cos(bobTimer * 0.5f) * bobbingAmount * 0.5f; // subtle side-to-side

                cameraParent.localPosition = new Vector3(
                    bobX,
                    originalCameraParentHeight + bobY,
                    cameraParent.localPosition.z
                );
            }
        }
    }
}
