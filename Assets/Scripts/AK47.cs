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
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private EasyPeasyFirstPersonController.FirstPersonController playerController; // Reference to FPS controller

    [Header("=== SLIDE / BOLT ===")]
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
    [Range(0f, 1f)]
    [SerializeField] private float shootVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float reloadVolume = 0.8f;

    [Header("=== WEAPON POSITIONS ===")]
    [SerializeField] private Transform hipPosition;
    [SerializeField] private Transform aimPosition;
    [SerializeField] private Transform runPosition;
    [SerializeField] private Transform reloadPosition;

    [Header("=== AK-47 STATS ===")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.45f;
    [SerializeField] private float fireRateRPM = 600f;
    [SerializeField] private float range = 150f;

    [Header("=== RECOIL ===")]
    [SerializeField] private float recoilKickBack = 0.1f;
    [SerializeField] private float recoilRotation = 5f;
    [SerializeField] private float recoilRecoverySpeed = 8f;

    [Header("=== AIMING SETTINGS ===")]
    [Range(0f,1f)] public float aimingRecoilMultiplier = 0.5f; // Reduce recoil when aiming
    [Range(0f,1f)] public float aimingBobMultiplier = 0.3f; // Reduce bobbing when aiming

    [Header("=== SWAY ===")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== VIEW BOBBING ===")]
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobAmount = 0.1f;
    [SerializeField] private float aimBobAmount = 0.02f;
    [SerializeField] private float bobSpeed = 8f;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private bool canShoot = true;

    private Quaternion initialWeaponRotation;
    private Vector3 slideStartLocalPos;
    private Vector3 slideEndLocalPos;

    private Vector3 currentRecoilPosition;
    private Vector3 targetRecoilPosition;
    private Vector3 currentRecoilRotation;
    private Vector3 targetRecoilRotation;

    private KeyCode fireKey, aimKey, reloadKey, runKey, switchFireModeKey;
    private float bobTimer;
    private Vector3 initialGunLocalPos;

    void Start()
    {
        if (userSettings != null)
        {
            fireKey = userSettings.fireKey;
            aimKey = userSettings.aimKey;
            reloadKey = userSettings.reloadKey;
            runKey = userSettings.runKey;
            switchFireModeKey = userSettings.switchFireModeKey;
        }

        currentAmmo = magazineSize;
        initialWeaponRotation = transform.localRotation;

        if (slideTransform && slideEndTransform)
        {
            slideStartLocalPos = slideTransform.localPosition;
            slideEndLocalPos = slideEndTransform.localPosition;
        }

        nextFireTime = Time.time;

        if (gunHolder != null) initialGunLocalPos = gunHolder.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(switchFireModeKey))
        {
            fireMode = fireMode == FireMode.Single ? FireMode.Automatic : FireMode.Single;
            Debug.Log("Fire Mode: " + fireMode);
        }

        HandleInput();
        HandleWeaponSway();
        HandleRecoilAndPosition();
        ReturnSlideForward();
    }

    void HandleInput()
    {
        if (!Input.GetKey(fireKey)) canShoot = true;

        if (Input.GetKeyDown(reloadKey))
            StartCoroutine(Reload());

        if (fireMode == FireMode.Single)
        {
            if (canShoot && Input.GetKey(fireKey) && Time.time >= nextFireTime)
                Shoot();
        }
        else if (fireMode == FireMode.Automatic)
        {
            if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
                Shoot();
        }
    }

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        canShoot = false;
        currentAmmo--;
        nextFireTime = Time.time + (60f / fireRateRPM);

        float multiplier = Input.GetKey(aimKey) ? aimingRecoilMultiplier : 1f;

        targetRecoilPosition += Vector3.back * recoilKickBack * multiplier;
        targetRecoilRotation += new Vector3(
            -recoilRotation * multiplier,
            Random.Range(-recoilRotation / 2f, recoilRotation / 2f) * multiplier,
            0f
        );

        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndLocalPos;

        if (shellCasingPrefab && shellEjectPoint)
        {
            Quaternion shellRot = Quaternion.LookRotation(-playerCamera.transform.forward);
            GameObject shell = Instantiate(shellCasingPrefab, shellEjectPoint.position, shellRot);
            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb)
            {
                Vector3 ejectDir = shellEjectPoint.right * Random.Range(1.8f, 2.6f) + shellEjectPoint.forward * 0.3f;
                rb.AddForce(ejectDir, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            Destroy(shell, 5f);
        }

        if (muzzleFlashPrefab && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(flash, 0.05f);
        }

        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, shootVolume);
    }

    IEnumerator Reload()
    {
        if (currentAmmo == magazineSize || isReloading) yield break;

        isReloading = true;

        if (audioSource && reloadSound)
            audioSource.PlayOneShot(reloadSound, reloadVolume);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
    }

    void HandleWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        Vector3 sway = new Vector3(mouseY, -mouseX, 0f) * swayAmount;
        Quaternion targetRot = Quaternion.Euler(sway);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot * initialWeaponRotation, Time.deltaTime * swaySmooth);
    }

    void HandleRecoilAndPosition()
    {
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, recoilRecoverySpeed * Time.deltaTime);
        currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, targetRecoilRotation, recoilRecoverySpeed * Time.deltaTime);

        Transform target;
        bool aiming = Input.GetKey(aimKey);
        bool running = Input.GetKey(runKey) && Input.GetAxis("Vertical") > 0;

        if (isReloading && reloadPosition != null) target = reloadPosition;
        else if (running) target = runPosition;
        else if (aiming) target = aimPosition;
        else target = hipPosition;

        Vector3 targetPos = target.localPosition + currentRecoilPosition;
        Quaternion targetRot = target.localRotation * Quaternion.Euler(currentRecoilRotation);

        // Handle bobbing only when moving AND grounded
        if (playerController != null && playerController.isGrounded)
        {
            float moveInput = Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal"));
            if (moveInput > 0.01f)
            {
                float speed = running ? 1.5f : 1f;
                if (aiming) speed *= 0.5f;

                float bobAmount = walkBobAmount;
                if (running) bobAmount = runBobAmount;
                if (aiming) bobAmount = aimBobAmount * aimingBobMultiplier;

                bobTimer += Time.deltaTime * bobSpeed * speed;
                Vector3 bobOffset = new Vector3(
                    Mathf.Sin(bobTimer) * bobAmount,
                    Mathf.Cos(bobTimer * 2) * bobAmount,
                    0f
                );
                targetPos += bobOffset;
            }
        }

        gunHolder.localPosition = Vector3.Lerp(gunHolder.localPosition, targetPos, Time.deltaTime * positionLerpSpeed);
        gunHolder.localRotation = Quaternion.Slerp(gunHolder.localRotation, targetRot, Time.deltaTime * positionLerpSpeed);

        targetRecoilPosition = Vector3.zero;
        targetRecoilRotation = Vector3.zero;
    }

    void ReturnSlideForward()
    {
        if (!slideTransform) return;
        slideTransform.localPosition = Vector3.Lerp(slideTransform.localPosition, slideStartLocalPos, Time.deltaTime * slideReturnSpeed);
    }
}
