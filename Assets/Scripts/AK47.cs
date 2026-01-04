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

    [Header("=== LOOK ===")]
    [SerializeField] private float verticalRotationLimit = 80f;

    [Header("=== LERP ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    int currentAmmo;
    float nextFireTime;
    bool canShoot = true;
    bool isReloading;

    float verticalRotation;
    float bobTimer;

    Vector3 recoilPos, recoilPosTarget;
    Vector3 recoilRot, recoilRotTarget;

    Vector3 slideStartPos;
    Quaternion initialWeaponRotation;

    KeyCode fireKey, aimKey, reloadKey, runKey, crouchKey, switchFireModeKey, moveForward, moveBackward, moveLeft, moveRight;

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

    // ================= INPUT =================

    void HandleFireModeSwitch()
    {
        if (Input.GetKeyDown(switchFireModeKey))
            fireMode = fireMode == FireMode.Single ? FireMode.Automatic : FireMode.Single;
    }

    void HandleInput()
    {
        if (!Input.GetKey(fireKey)) canShoot = true;

        if (Input.GetKeyDown(reloadKey))
            StartCoroutine(Reload());

        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            if (fireMode == FireMode.Automatic || canShoot)
                Shoot();
        }
    }

    // ================= LOOK =================

    void UpdateVerticalLook()
    {
        verticalRotation -= Input.GetAxis("Mouse Y");
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
    }

    // ================= SHOOT =================

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        canShoot = false;
        currentAmmo--;
        nextFireTime = Time.time + (60f / fireRateRPM);

        recoilPosTarget += Vector3.back * recoilKickBack;
        recoilRotTarget += new Vector3(-recoilRotation, Random.Range(-recoilRotation, recoilRotation), 0f);

        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndTransform.localPosition;

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

    // ================= VISUALS =================

    void HandleSway()
    {
        Vector3 sway = new Vector3(
            Input.GetAxis("Mouse Y"),
            -Input.GetAxis("Mouse X"),
            0f
        ) * swayAmount;

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

        // 🏃 RUN ONLY IF MOVING
        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
        bool running = Input.GetKey(runKey) && Input.GetKey(moveForward) && !Input.GetKey(moveBackward) || Input.GetKey(runKey) && Input.GetKey(moveForward) && Input.GetKey(moveLeft) && !Input.GetKey(moveBackward) || Input.GetKey(runKey) && Input.GetKey(moveForward) && Input.GetKey(moveRight) && !Input.GetKey(moveBackward);

        Transform target =
            isReloading && !crouching ? reloadPosition :
            isReloading && crouching ? crouchReloadPosition :
            running ? runPosition :
            aiming && crouching ? crouchAimPosition :
            aiming ? aimPosition :
            crouching ? crouchHipPosition :
            hipPosition;

        // Recoil
        recoilPos = Vector3.Lerp(recoilPos, recoilPosTarget, recoilRecoverySpeed * Time.deltaTime);
        recoilRot = Vector3.Lerp(recoilRot, recoilRotTarget, recoilRecoverySpeed * Time.deltaTime);

        recoilPosTarget = Vector3.zero;
        recoilRotTarget = Vector3.zero;

        Vector3 finalPos = target.localPosition + recoilPos;

        // Bob
        if (playerController && playerController.isGrounded && isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed * (running ? 1.5f : 1f);
            float bob = running ? runBob : walkBob;
            finalPos += new Vector3(Mathf.Sin(bobTimer) * bob, Mathf.Cos(bobTimer * 2) * bob, 0);
        }

        gunHolder.localPosition = Vector3.Lerp(
            gunHolder.localPosition,
            finalPos,
            Time.deltaTime * positionLerpSpeed
        );

        // 🔒 ROTATION LOCK
        if (verticalPivot)
        {
            if (running || isReloading)
                verticalPivot.localRotation = Quaternion.identity;
            else
                verticalPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        gunHolder.localRotation = Quaternion.Slerp(
            gunHolder.localRotation,
            target.localRotation * Quaternion.Euler(recoilRot),
            Time.deltaTime * positionLerpSpeed
        );
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
