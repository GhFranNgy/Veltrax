using UnityEngine;
using System.Collections;

public class AK47 : MonoBehaviour
{
    public enum FireMode { Single, Automatic }

    [Header("=== FIRE MODE ===")]
    public FireMode fireMode = FireMode.Single;

    [Header("=== USER SETTINGS ===")]
    public UserSettings userSettings;

    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Transform verticalPivot;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private EasyPeasyFirstPersonController.FirstPersonController playerController;

    [Header("=== SLIDE ===")]
    [SerializeField] private Transform slideTransform;
    [SerializeField] private Transform slideEndTransform;
    [SerializeField] private float slideReturnSpeed = 18f;

    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [Range(0f, 1f)] public float shootVolume = 0.8f;
    [Range(0f, 1f)] public float reloadVolume = 0.8f;

    [Header("=== WEAPON POSITIONS ===")]
    [SerializeField] private Transform hipPosition;
    [SerializeField] private Transform aimPosition;
    [SerializeField] private Transform crouchHipPosition;
    [SerializeField] private Transform crouchAimPosition;
    [SerializeField] private Transform crouchWalkAimPosition;
    [SerializeField] private Transform reloadPosition;
    [SerializeField] private Transform crouchReloadPosition;
    [SerializeField] private Transform runPosition;

    [Header("=== STATS ===")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.45f;
    [SerializeField] private float fireRateRPM = 600f;

    [Header("=== RECOIL ===")]
    [SerializeField] private float recoilKickBack = 0.1f;
    [SerializeField] private float recoilRotation = 5f;
    [SerializeField] private float recoilRecoverySpeed = 8f;

    [Header("=== SWAY ===")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== BOB ===")]
    [SerializeField] private float walkBob = 0.05f;
    [SerializeField] private float runBob = 0.1f;
    [SerializeField] private float bobSpeed = 8f;

    [Header("=== EFFECT MULTIPLIERS ===")]
    [SerializeField] private float aimBobMultiplier = 0.3f;
    [SerializeField] private float walkBobMultiplier = 1f;
    [SerializeField] private float aimRecoilMultiplier = 0.5f;

    [Header("=== LOOK ===")]
    [SerializeField] private float verticalRotationLimit = 80f;

    [Header("=== LERP ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    [Header("=== RECOIL TARGET ===")]
    [SerializeField] private Transform recoilTarget;

    int currentAmmo;
    float nextFireTime;
    bool canShoot = true;
    bool isReloading;

    float verticalRotation;
    float bobTimer;

    Vector3 slideStartPos;
    Quaternion initialWeaponRotation;

    KeyCode fireKey, aimKey, reloadKey, runKey, crouchKey, switchFireModeKey;
    KeyCode moveForward, moveBackward, moveLeft, moveRight;

    void Start()
    {
        if (userSettings)
        {
            fireKey = userSettings.fireKey;
            aimKey = userSettings.aimKey;
            reloadKey = userSettings.reloadKey;
            runKey = userSettings.runKey;
            crouchKey = userSettings.crouchKey;
            switchFireModeKey = userSettings.switchFireModeKey;

            moveLeft = userSettings.moveLeft;
            moveRight = userSettings.moveRight;
            moveForward = userSettings.moveForward;
            moveBackward = userSettings.moveBackward;
        }

        currentAmmo = magazineSize;
        initialWeaponRotation = transform.localRotation;

        if (slideTransform)
            slideStartPos = slideTransform.localPosition;

        if (recoilTarget)
        {
            recoilTarget.localPosition = Vector3.zero;
            recoilTarget.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        HandleFireModeSwitch();
        HandleInput();
        UpdateVerticalLook();
        HandleSway();
        HandleWeaponTransform();
        ReturnSlide();
    }

    void HandleFireModeSwitch()
    {
        if (Input.GetKeyDown(switchFireModeKey))
            fireMode = fireMode == FireMode.Single ? FireMode.Automatic : FireMode.Single;
    }

    void HandleInput()
    {
        if (!Input.GetKey(fireKey))
            canShoot = true;

        if (Input.GetKeyDown(reloadKey))
            StartCoroutine(Reload());

        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            if (fireMode == FireMode.Automatic || canShoot)
                Shoot();
        }
    }

    void UpdateVerticalLook()
    {
        if (!playerCamera || !verticalPivot) return;

        float cameraPitch = playerCamera.transform.localEulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;

        verticalRotation = Mathf.Clamp(cameraPitch, -verticalRotationLimit, verticalRotationLimit);
    }

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        canShoot = false;
        currentAmmo--;
        nextFireTime = Time.time + (60f / fireRateRPM);

        float recoilMult = Input.GetKey(aimKey) ? aimRecoilMultiplier : 1f;

        if (recoilTarget)
        {
            recoilTarget.localPosition += Vector3.back * recoilKickBack * recoilMult;
            recoilTarget.localRotation *= Quaternion.Euler(
                -recoilRotation * recoilMult,
                Random.Range(-recoilRotation, recoilRotation) * recoilMult,
                0f
            );
        }

        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndTransform.localPosition;

        // === SHELL EJECTION (USING PROVIDED MECHANISM) ===
        if (shellCasingPrefab && shellEjectPoint)
        {
            Quaternion shellRot = Quaternion.LookRotation(-playerCamera.transform.forward);
            GameObject shell = Instantiate(shellCasingPrefab, shellEjectPoint.position, shellRot);

            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb)
            {
                Vector3 force =
                    shellEjectPoint.right * Random.Range(2f, 3f) +
                    shellEjectPoint.forward * 0.4f;

                rb.AddForce(force, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }

            Destroy(shell, 1f);
        }

        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, shootVolume);
    }

    IEnumerator Reload()
    {
        if (isReloading || currentAmmo == magazineSize) yield break;

        isReloading = true;

        if (audioSource && reloadSound)
            audioSource.PlayOneShot(reloadSound, reloadVolume);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
    }

    void HandleSway()
    {
        float mouseX = Input.GetAxis(userSettings.mouseXAxis);
        float mouseY = Input.GetAxis(userSettings.mouseYAxis);

        Vector3 sway = new Vector3(mouseY, -mouseX, 0f) * swayAmount;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            Quaternion.Euler(sway) * initialWeaponRotation,
            Time.deltaTime * swaySmooth
        );
    }

    void HandleWeaponTransform()
    {
        bool aiming = Input.GetKey(aimKey);
        bool crouching = Input.GetKey(crouchKey);

        float h = Input.GetAxis(userSettings.horizontalAxis);
        float v = Input.GetAxis(userSettings.verticalAxis);
        bool isMoving = Mathf.Abs(h) + Mathf.Abs(v) > 0.1f;

        bool running =
            Input.GetKey(runKey) &&
            Input.GetKey(moveForward) &&
            !Input.GetKey(moveBackward);

        Transform target =
            isReloading && !crouching ? reloadPosition :
            isReloading && crouching ? crouchReloadPosition :
            running ? runPosition :
            aiming && isMoving && crouching ? crouchWalkAimPosition :
            aiming && crouching ? crouchAimPosition :
            aiming ? aimPosition :
            crouching ? crouchHipPosition :
            hipPosition;

        if (recoilTarget)
        {
            gunHolder.localPosition = Vector3.Lerp(
                gunHolder.localPosition,
                target.localPosition + recoilTarget.localPosition,
                Time.deltaTime * positionLerpSpeed
            );

            gunHolder.localRotation = Quaternion.Slerp(
                gunHolder.localRotation,
                target.localRotation * recoilTarget.localRotation,
                Time.deltaTime * positionLerpSpeed
            );

            recoilTarget.localPosition = Vector3.Lerp(
                recoilTarget.localPosition,
                Vector3.zero,
                Time.deltaTime * recoilRecoverySpeed
            );

            recoilTarget.localRotation = Quaternion.Slerp(
                recoilTarget.localRotation,
                Quaternion.identity,
                Time.deltaTime * recoilRecoverySpeed
            );
        }

        if (verticalPivot)
        {
            if (running || isReloading)
                verticalPivot.localRotation = Quaternion.identity;
            else
                verticalPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    void ReturnSlide()
    {
        if (!slideTransform) return;

        slideTransform.localPosition = Vector3.Lerp(
            slideTransform.localPosition,
            slideStartPos,
            Time.deltaTime * slideReturnSpeed
        );
    }
}
