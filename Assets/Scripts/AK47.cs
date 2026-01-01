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
    [SerializeField] private EasyPeasyFirstPersonController.FirstPersonController playerController;

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
    [Range(0f, 1f)] [SerializeField] private float shootVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float reloadVolume = 0.8f;

    [Header("=== AK-47 STATS ===")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.45f;
    [SerializeField] private float fireRateRPM = 600f;

    [Header("=== SWAY ===")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== VIEW BOBBING ===")]
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobAmount = 0.1f;
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

    private KeyCode fireKey, reloadKey, runKey, switchFireModeKey;
    private float bobTimer;
    private Vector3 initialGunLocalPos;

    void Start()
    {
        if (userSettings != null)
        {
            fireKey = userSettings.fireKey;
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

        if (gunHolder != null)
            initialGunLocalPos = gunHolder.localPosition;
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
        HandleBobbing();
        ReturnSlideForward();
    }

    void HandleInput()
    {
        if (!Input.GetKey(fireKey))
            canShoot = true;

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

        // Slide movement
        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndLocalPos;

        // Shell ejection (ROTATED 180°)
        if (shellCasingPrefab && shellEjectPoint)
        {
            // Rotate shell 180 degrees so it faces away from you
            Quaternion shellRot = Quaternion.LookRotation(transform.forward * -1f, transform.up);

            GameObject shell = Instantiate(shellCasingPrefab, shellEjectPoint.position, shellRot);
            Rigidbody rb = shell.GetComponent<Rigidbody>();

            if (rb)
            {
                Vector3 ejectDir =
                    shellEjectPoint.right * Random.Range(1.8f, 2.6f) +
                    shellEjectPoint.up * 0.2f;

                rb.AddForce(ejectDir, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }

            Destroy(shell, 5f);
        }

        // Muzzle flash
        if (muzzleFlashPrefab && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(flash, 0.05f);
        }

        // Sound
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

    void HandleBobbing()
    {
        Vector3 targetPos = initialGunLocalPos;

        if (playerController != null && playerController.isGrounded)
        {
            float moveInput = Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal"));
            if (moveInput > 0.01f)
            {
                float bobAmount = Input.GetKey(runKey) ? runBobAmount : walkBobAmount;

                bobTimer += Time.deltaTime * bobSpeed;
                Vector3 bobOffset = new Vector3(
                    Mathf.Sin(bobTimer) * bobAmount,
                    Mathf.Cos(bobTimer * 2) * bobAmount,
                    0f
                );
                targetPos += bobOffset;
            }
        }

        gunHolder.localPosition = Vector3.Lerp(gunHolder.localPosition, targetPos, Time.deltaTime * positionLerpSpeed);
    }

    void ReturnSlideForward()
    {
        if (!slideTransform) return;
        slideTransform.localPosition = Vector3.Lerp(slideTransform.localPosition, slideStartLocalPos, Time.deltaTime * slideReturnSpeed);
    }
}
