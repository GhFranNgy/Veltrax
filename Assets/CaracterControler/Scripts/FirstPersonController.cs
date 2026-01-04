using UnityEngine;

namespace EasyPeasyFirstPersonController
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // ================= REFERENCES =================

        [Header("References")]
        public UserSettings userSettings;
        public Transform playerCamera;
        public Transform cameraParent;
        public Transform groundCheck;
        public Animator animator;

        [Header("Body Parts")]
        public Transform neckBone;

        // ================= LOOK =================

        [Header("Vertical Rotation Limit")]
        public float minPitch = -70f;
        public float maxPitch = 70f;

        [Header("Neck Rotation")]
        public float neckRotationLimit = 40f;

        [Header("Look Smoothing")]
        [Range(0f, 200f)] public float snappiness = 100f;

        // ================= MOVEMENT =================

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 5f;
        public float crouchSpeed = 1.5f;
        public float aimWalkSpeed = 1.5f;
        public float jumpSpeed = 3f;
        public float gravity = 9.81f;

        // ================= CROUCH & SLIDE =================

        [Header("Crouch & Slide")]
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 1f;
        public float slideSpeed = 8f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;

        // ================= COYOTE TIME =================

        [Header("Coyote Time")]
        public bool coyoteTimeEnabled = true;
        [Range(0.01f, 0.3f)] public float coyoteTimeDuration = 0.2f;

        // ================= FOV =================

        [Header("FOV")]
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float adsFov = 45f;
        public float fovChangeSpeed = 5f;
        public float adsFovSpeed = 10f;

        // ================= HEAD BOB =================

        [Header("Head Bobbing")]
        public float walkingBobbingSpeed = 10f;
        public float bobbingAmount = 0.05f;
        public float sprintBobMultiplier = 1.5f;

        // ================= GROUND CHECK =================

        [Header("Ground Check")]
        public float groundDistance = 0.2f;
        public LayerMask groundMask;
        public QueryTriggerInteraction groundCheckQuery = QueryTriggerInteraction.Ignore;

        // ================= FEATURES =================

        [Header("Features")]
        public bool canSprint = true;
        public bool canJump = true;
        public bool canCrouch = true;
        public bool canSlide = true;

        // ================= STATE =================

        public bool isGrounded { get; private set; }
        public bool isSprinting { get; private set; }
        public bool isCrouching { get; private set; }
        public bool isSliding { get; private set; }

        // ================= INTERNAL =================

        private CharacterController controller;
        private Camera cam;

        private Vector3 moveDirection;
        private Vector3 slideDirection;

        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private float neckVerticalRotation;

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
                animator = GetComponentInChildren<Animator>();

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
            UpdateAnimator();
        }

        private void LateUpdate()
        {
            HandleNeckRotation();
        }

        // ================= LOOK =================

        private void HandleLook()
        {
            float sensitivity = userSettings.mouseSensitivity;

            if (Input.GetKey(userSettings.aimKey) && !isSprinting)
                sensitivity *= userSettings.adsSensitivityMultiplier;

            float mouseX = Input.GetAxis(userSettings.mouseXAxis);
            float mouseY = Input.GetAxis(userSettings.mouseYAxis);

            if (userSettings.invertMouseY)
                mouseY *= -1f;

            rotX += mouseX * sensitivity * Time.deltaTime * 10f;
            rotY -= mouseY * sensitivity * Time.deltaTime * 10f;
            rotY = Mathf.Clamp(rotY, minPitch, maxPitch);

            xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
            yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

            float targetTilt = isSliding ? slideTiltAngle : 0f;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, 0.15f);

            playerCamera.localRotation = Quaternion.Euler(yVelocity - currentTilt, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
        }

        // ================= NECK =================

		private void HandleNeckRotation()
		{
				if (neckBone == null || playerCamera == null) return;

				float cameraPitch = playerCamera.localEulerAngles.x;

				// Convert 0–360 to -180–180
				if (cameraPitch > 180f)
						cameraPitch -= 360f;

				neckVerticalRotation = Mathf.Clamp(
						cameraPitch,
						-neckRotationLimit,
						neckRotationLimit
				);

				neckBone.localRotation = Quaternion.Euler(neckVerticalRotation, 0f, 0f);
		}


        // ================= GROUND CHECK =================

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
                if (moveDirection.y < 0f)
                    moveDirection.y = -2f;

                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        // ================= MOVEMENT =================

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
                !isSliding;

            float speed =
                isSprinting ? sprintSpeed :
                isCrouching ? crouchSpeed :
                (Input.GetKey(userSettings.aimKey) ? aimWalkSpeed : walkSpeed);

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

        // ================= CROUCH & SLIDE =================

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

        // ================= HEAD BOB =================

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

        // ================= FOV =================

        private void HandleFov()
        {
            float targetFov = normalFov;

            if (isSliding)
                targetFov = sprintFov + slideFovBoost;
            else if (isSprinting)
                targetFov = sprintFov;
            else if (Input.GetKey(userSettings.aimKey))
                targetFov = adsFov;

            float speed = (!isSprinting && Input.GetKey(userSettings.aimKey))
                ? adsFovSpeed
                : fovChangeSpeed;

            currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, 1f / speed);
            cam.fieldOfView = currentFov;
        }

        // ================= ANIMATOR =================

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
            animator.SetBool("isAiming", Input.GetKey(userSettings.aimKey) && !isSprinting);
        }
    }
}
