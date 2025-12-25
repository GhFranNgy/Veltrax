using UnityEngine;
using System.Collections;

public class AK47 : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private Transform slideTransform;

    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [Range(0f, 1f)]
    [SerializeField] private float shootVolume = 0.8f;

    [Header("=== WEAPON POSITIONS ===")]
    [SerializeField] private Transform hipPosition;
    [SerializeField] private Transform aimPosition;
    [SerializeField] private Transform runPosition;

    [Header("=== AK-47 STATS (REALISTIC) ===")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.45f;
    [SerializeField] private float fireRateRPM = 600f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float range = 150f;

    [Header("=== RECOIL & SWAY ===")]
    [SerializeField] private float recoilKick = 1.5f;
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== SLIDE SETTINGS ===")]
    [SerializeField] private float slideKickDistance = 0.08f;
    [SerializeField] private float slideReturnSpeed = 18f;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;

    private Quaternion initialRotation;
    private Vector3 slideStartPos;
    private Vector3 slideBackPos;

    void Start()
    {
        currentAmmo = magazineSize;
        initialRotation = transform.localRotation;

        if (slideTransform)
        {
            slideStartPos = slideTransform.localPosition;
            slideBackPos = slideStartPos - Vector3.forward * slideKickDistance;
        }
    }

    void Update()
    {
        if (isReloading) return;

        HandleInput();
        HandleWeaponSway();
        HandleWeaponPosition();
        ReturnSlide();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartCoroutine(Reload());

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            Shoot();
    }

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        nextFireTime = Time.time + 60f / fireRateRPM;

        // Raycast
        if (Physics.Raycast(playerCamera.transform.position,
            playerCamera.transform.forward, out RaycastHit hit, range))
        {
            // hit.collider.GetComponent<Health>()?.TakeDamage(damage);
        }

        // Camera recoil
        playerCamera.transform.Rotate(-recoilKick, 0f, 0f);

        // Slide kick
        if (slideTransform)
            slideTransform.localPosition = slideBackPos;

        // Shell casing
        if (shellCasingPrefab && shellEjectPoint)
        {
            GameObject shell = Instantiate(
                shellCasingPrefab,
                shellEjectPoint.position,
                shellEjectPoint.rotation
            );

            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb)
                rb.AddForce(shellEjectPoint.right * Random.Range(1.5f, 2.5f), ForceMode.Impulse);

            Destroy(shell, 5f);
        }

        // Muzzle flash
        if (muzzleFlashPrefab && muzzlePoint)
        {
            GameObject flash = Instantiate(
                muzzleFlashPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation,
                muzzlePoint
            );
            Destroy(flash, 0.05f);
        }

        // Shoot sound
        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, shootVolume);
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

    IEnumerator Reload()
    {
        if (currentAmmo == magazineSize) yield break;

        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
    }

    void HandleWeaponSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 sway = new Vector3(-mouseY, mouseX, 0f) * swayAmount;
        Quaternion targetRot = Quaternion.Euler(sway);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot * initialRotation,
            Time.deltaTime * swaySmooth
        );
    }

    void HandleWeaponPosition()
    {
        Transform target = hipPosition;

        bool aiming = Input.GetButton("Fire2");
        bool running = Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0;

        if (running)
            target = runPosition;
        else if (aiming)
            target = aimPosition;

        gunHolder.localPosition = Vector3.Lerp(
            gunHolder.localPosition,
            target.localPosition,
            Time.deltaTime * positionLerpSpeed
        );

        gunHolder.localRotation = Quaternion.Slerp(
            gunHolder.localRotation,
            target.localRotation,
            Time.deltaTime * positionLerpSpeed
        );
    }
}