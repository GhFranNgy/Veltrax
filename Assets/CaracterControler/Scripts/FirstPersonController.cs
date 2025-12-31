/*using UnityEngine;

namespace EasyPeasyFirstPersonController
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // ================= SETTINGS =================

        [Header("References")]
        public UserSettings userSettings;
        public Transform playerCamera;
        public Transform cameraParent;
        public Transform groundCheck;

        [Header("Mouse & Look")]
        [Range(0, 100)] public float mouseSensitivity = 50f;
        [Range(0f, 200f)] public float snappiness = 100f;

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float aimWalkSpeed = 1.5f;
        public float jumpSpeed = 3f;
        public float gravity = 9.81f;

        [Header("Crouch & Slide")]
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 1f;
        public float slideSpeed = 8f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;

        [Header("Coyote Time")]
        public bool coyoteTimeEnabled = true;
        [Range(0.01f, 0.3f)] public float coyoteTimeDuration = 0.2f;

        [Header("FOV")]
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float adsFov = 45f;
        public float fovChangeSpeed = 5f;
        public float adsFovSpeed = 10f;

        [Header("Head Bobbing")]
        public float walkingBobbingSpeed = 10f;
        public float bobbingAmount = 0.05f;
        public float sprintBobMultiplier = 1.5f;

        [Header("Ground Check")]
        public float groundDistance = 0.2f;
        public LayerMask groundMask;
        public QueryTriggerInteraction groundCheckQuery = QueryTriggerInteraction.Ignore;

        [Header("Features")]
        public bool canSprint = true;
        public bool canJump = true;
        public bool canCrouch = true;
        public bool canSlide = true;

        // ================= INTERNAL =================

        public bool isGrounded { get; private set; }
        public bool isSprinting { get; private set; }
        public bool isCrouching { get; private set; }
        public bool isSliding { get; private set; }

        private CharacterController controller;
        private Camera cam;

        private Vector3 moveDirection;
        private Vector3 slideDirection;

        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private float coyoteTimer;
        private float slideTimer;
        private float currentFov;
        private float fovVelocity;
        private float currentTilt;
        private float tiltVelocity;
        private float bobTimer;

        private float originalHeight;
        private float originalCameraY;

        // Camera smoothing
        private float currentCameraYPos;
        private float cameraLerpVelocity;

        // ================= UNITY =================

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            cam = playerCamera.GetComponent<Camera>();

            originalHeight = controller.height;
            originalCameraY = cameraParent.localPosition.y;

            currentCameraYPos = originalCameraY;
            currentFov = normalFov;

            rotX = transform.rotation.eulerAngles.y;
            rotY = playerCamera.localRotation.eulerAngles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleLook();
            HandleMovement();
            HandleCrouchAndSlide();
            HandleHeadBob();
            HandleFov();
        }

        // ================= SYSTEMS =================

        private void HandleGroundCheck()
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask,
                groundCheckQuery
            );

            if (isGrounded)
            {
                if (moveDirection.y < 0)
                    moveDirection.y = -2f;

                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        private void HandleLook()
        {
            float sensitivity = mouseSensitivity;
            if (Input.GetKey(userSettings.aimKey))
                sensitivity *= 0.6f;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime * 10f;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * 10f;

            rotX += mouseX;
            rotY -= mouseY;
            rotY = Mathf.Clamp(rotY, -90f, 90f);

            xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
            yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

            float targetTilt = isSliding ? slideTiltAngle : 0f;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, 0.15f);

            playerCamera.localRotation = Quaternion.Euler(yVelocity - currentTilt, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
        }

        private void HandleMovement()
        {
            Vector2 input = Vector2.zero;

            if (Input.GetKey(userSettings.moveForward)) input.y += 1f;
            if (Input.GetKey(userSettings.moveBackward)) input.y -= 1f;
            if (Input.GetKey(userSettings.moveRight)) input.x += 1f;
            if (Input.GetKey(userSettings.moveLeft)) input.x -= 1f;

            isSprinting =
                canSprint &&
                Input.GetKey(userSettings.runKey) &&
                input.y > 0.1f &&
                isGrounded &&
                !isCrouching &&
                !isSliding &&
                !Input.GetKey(userSettings.aimKey);

            float speed =
                isSprinting ? sprintSpeed :
                Input.GetKey(userSettings.aimKey) ? aimWalkSpeed :
                isCrouching ? crouchSpeed :
                walkSpeed;

            Vector3 move = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;

            if ((isGrounded || coyoteTimer > 0f) &&
                canJump &&
                Input.GetKeyDown(userSettings.jumpKey) &&
                !isSliding)
            {
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
                controller.Move(moveDirection * Time.deltaTime);
            }
        }

        private void HandleCrouchAndSlide()
        {
            bool wantsCrouch = canCrouch && Input.GetKey(userSettings.crouchKey) && !isSliding;

            // --- SLIDE START ---
            if (canSlide &&
                isSprinting &&
                Input.GetKeyDown(userSettings.slideKey) &&
                isGrounded)
            {
                isSliding = true;
                slideTimer = slideDuration;
                slideDirection = transform.forward;
            }

            // --- SLIDE MOVEMENT ---
            if (isSliding)
            {
                slideTimer -= Time.deltaTime;

                if (slideTimer <= 0f || !isGrounded)
                    isSliding = false;

                controller.Move(slideDirection * slideSpeed * Time.deltaTime);
            }

            // --- CROUCH STATE ---
            isCrouching = wantsCrouch || isSliding;

            // --- CHARACTER CONTROLLER RESIZE (BOTTOM ANCHORED) ---
            float targetHeight = isCrouching ? crouchHeight : originalHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

            // --- CAMERA HEIGHT (SMOOTH LIKE SCRIPT #1) ---
            float targetCamY = isCrouching || isSliding ? crouchCameraHeight : originalCameraY;

            currentCameraYPos = Mathf.SmoothDamp(
                currentCameraYPos,
                targetCamY,
                ref cameraLerpVelocity,
                0.1f
            );

            cameraParent.localPosition = new Vector3(
                cameraParent.localPosition.x,
                currentCameraYPos,
                cameraParent.localPosition.z
            );
        }

        private void HandleHeadBob()
        {
            if (!isGrounded || isSliding || isCrouching || Input.GetKey(userSettings.aimKey))
            {
                bobTimer = 0f;
                return;
            }

            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            if (horizontalVelocity.magnitude < 0.1f) return;

            float speed = walkingBobbingSpeed * (isSprinting ? sprintBobMultiplier : 1f);
            bobTimer += Time.deltaTime * speed;

            float bobY = Mathf.Sin(bobTimer) * bobbingAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobbingAmount * 0.5f;

            cameraParent.localPosition = new Vector3(
                bobX,
                currentCameraYPos + bobY,
                0f
            );
        }

        private void HandleFov()
        {
            float targetFov = normalFov;

            if (isSliding)
                targetFov = sprintFov + slideFovBoost;
            else if (Input.GetKey(userSettings.aimKey))
                targetFov = adsFov;
            else if (isSprinting)
                targetFov = sprintFov;

            float speed = Input.GetKey(userSettings.aimKey) ? adsFovSpeed : fovChangeSpeed;

            currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, 1f / speed);
            cam.fieldOfView = currentFov;
        }
    }
}*/
using UnityEngine;

namespace EasyPeasyFirstPersonController
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // ================= SETTINGS =================

        [Header("References")]
        public UserSettings userSettings;
        public Transform playerCamera;
        public Transform cameraParent;
        public Transform groundCheck;
        public Animator animator; // ⭐ ADDED

        [Header("Mouse & Look")]
        [Range(0, 100)] public float mouseSensitivity = 50f;
        [Range(0f, 200f)] public float snappiness = 100f;

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float aimWalkSpeed = 1.5f;
        public float jumpSpeed = 3f;
        public float gravity = 9.81f;

        [Header("Crouch & Slide")]
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 1f;
        public float slideSpeed = 8f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;

        [Header("Coyote Time")]
        public bool coyoteTimeEnabled = true;
        [Range(0.01f, 0.3f)] public float coyoteTimeDuration = 0.2f;

        [Header("FOV")]
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float adsFov = 45f;
        public float fovChangeSpeed = 5f;
        public float adsFovSpeed = 10f;

        [Header("Head Bobbing")]
        public float walkingBobbingSpeed = 10f;
        public float bobbingAmount = 0.05f;
        public float sprintBobMultiplier = 1.5f;

        [Header("Ground Check")]
        public float groundDistance = 0.2f;
        public LayerMask groundMask;
        public QueryTriggerInteraction groundCheckQuery = QueryTriggerInteraction.Ignore;

        [Header("Features")]
        public bool canSprint = true;
        public bool canJump = true;
        public bool canCrouch = true;
        public bool canSlide = true;

        // ================= INTERNAL =================

        public bool isGrounded { get; private set; }
        public bool isSprinting { get; private set; }
        public bool isCrouching { get; private set; }
        public bool isSliding { get; private set; }

        private CharacterController controller;
        private Camera cam;

        private Vector3 moveDirection;
        private Vector3 slideDirection;

        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private float coyoteTimer;
        private float slideTimer;
        private float currentFov;
        private float fovVelocity;
        private float currentTilt;
        private float tiltVelocity;
        private float bobTimer;

        private float originalHeight;
        private float originalCameraY;

        private float currentCameraYPos;
        private float cameraLerpVelocity;

        // ================= UNITY =================

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            cam = playerCamera.GetComponent<Camera>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(); // ⭐ AUTO-FIND

            originalHeight = controller.height;
            originalCameraY = cameraParent.localPosition.y;

            currentCameraYPos = originalCameraY;
            currentFov = normalFov;

            rotX = transform.rotation.eulerAngles.y;
            rotY = playerCamera.localRotation.eulerAngles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleLook();
            HandleMovement();
            HandleCrouchAndSlide();
            HandleHeadBob();
            HandleFov();
            UpdateAnimator(); // ⭐ ADDED
        }

        // ================= SYSTEMS =================

        private void HandleGroundCheck()
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask,
                groundCheckQuery
            );

            if (isGrounded)
            {
                if (moveDirection.y < 0)
                    moveDirection.y = -2f;

                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        private void HandleLook()
        {
            float sensitivity = mouseSensitivity;
            if (Input.GetKey(userSettings.aimKey))
                sensitivity *= 0.6f;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime * 10f;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * 10f;

            rotX += mouseX;
            rotY -= mouseY;
            rotY = Mathf.Clamp(rotY, -90f, 90f);

            xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
            yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

            float targetTilt = isSliding ? slideTiltAngle : 0f;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, 0.15f);

            playerCamera.localRotation = Quaternion.Euler(yVelocity - currentTilt, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
        }

        private void HandleMovement()
        {
            Vector2 input = Vector2.zero;

            if (Input.GetKey(userSettings.moveForward)) input.y += 1f;
            if (Input.GetKey(userSettings.moveBackward)) input.y -= 1f;
            if (Input.GetKey(userSettings.moveRight)) input.x += 1f;
            if (Input.GetKey(userSettings.moveLeft)) input.x -= 1f;

            isSprinting =
                canSprint &&
                Input.GetKey(userSettings.runKey) &&
                input.y > 0.1f &&
                isGrounded &&
                !isCrouching &&
                !isSliding &&
                !Input.GetKey(userSettings.aimKey);

            float speed =
                isSprinting ? sprintSpeed :
                Input.GetKey(userSettings.aimKey) ? aimWalkSpeed :
                isCrouching ? crouchSpeed :
                walkSpeed;

            Vector3 move = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;

            if ((isGrounded || coyoteTimer > 0f) &&
                canJump &&
                Input.GetKeyDown(userSettings.jumpKey) &&
                !isSliding)
            {
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
                controller.Move(moveDirection * Time.deltaTime);
            }
        }

        private void HandleCrouchAndSlide()
        {
            bool wantsCrouch = canCrouch && Input.GetKey(userSettings.crouchKey) && !isSliding;

            if (canSlide &&
                isSprinting &&
                Input.GetKeyDown(userSettings.slideKey) &&
                isGrounded)
            {
                isSliding = true;
                slideTimer = slideDuration;
                slideDirection = transform.forward;
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;

                if (slideTimer <= 0f || !isGrounded)
                    isSliding = false;

                controller.Move(slideDirection * slideSpeed * Time.deltaTime);
            }

            isCrouching = wantsCrouch || isSliding;

            float targetHeight = isCrouching ? crouchHeight : originalHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

            float targetCamY = isCrouching || isSliding ? crouchCameraHeight : originalCameraY;

            currentCameraYPos = Mathf.SmoothDamp(
                currentCameraYPos,
                targetCamY,
                ref cameraLerpVelocity,
                0.1f
            );

            cameraParent.localPosition = new Vector3(
                cameraParent.localPosition.x,
                currentCameraYPos,
                cameraParent.localPosition.z
            );
        }

        private void HandleHeadBob()
        {
            if (!isGrounded || isSliding || isCrouching || Input.GetKey(userSettings.aimKey))
            {
                bobTimer = 0f;
                return;
            }

            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            if (horizontalVelocity.magnitude < 0.1f) return;

            float speed = walkingBobbingSpeed * (isSprinting ? sprintBobMultiplier : 1f);
            bobTimer += Time.deltaTime * speed;

            float bobY = Mathf.Sin(bobTimer) * bobbingAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobbingAmount * 0.5f;

            cameraParent.localPosition = new Vector3(
                bobX,
                currentCameraYPos + bobY,
                0f
            );
        }

        private void HandleFov()
        {
            float targetFov = normalFov;

            if (isSliding)
                targetFov = sprintFov + slideFovBoost;
            else if (Input.GetKey(userSettings.aimKey))
                targetFov = adsFov;
            else if (isSprinting)
                targetFov = sprintFov;

            float speed = Input.GetKey(userSettings.aimKey) ? adsFovSpeed : fovChangeSpeed;

            currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, 1f / speed);
            cam.fieldOfView = currentFov;
        }

        // ================= ANIMATION =================

        private void UpdateAnimator()
        {
            if (animator == null) return;

            Vector2 input = Vector2.zero;
            if (Input.GetKey(userSettings.moveForward)) input.y += 1f;
            if (Input.GetKey(userSettings.moveBackward)) input.y -= 1f;
            if (Input.GetKey(userSettings.moveRight)) input.x += 1f;
            if (Input.GetKey(userSettings.moveLeft)) input.x -= 1f;

            animator.SetFloat("InputX", input.x);
            animator.SetFloat("InputY", input.y);

            animator.SetBool("isSprinting", isSprinting);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("isSliding", isSliding);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isJumping", moveDirection.y > 0.1f);
            animator.SetBool("isAiming", Input.GetKey(userSettings.aimKey));
        }
    }
}
