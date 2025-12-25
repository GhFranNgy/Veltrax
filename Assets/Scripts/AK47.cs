using UnityEngine;
using System.Collections;

public class AK47 : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Transform shellEjectPoint;

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
    [Range(0f, 1f)] [SerializeField] private float shootVolume = 0.8f;

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

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;

    private Quaternion initialRotation;
    private Vector3 slideStartLocalPos;
    private Vector3 slideEndLocalPos;

    void Start()
    {
        currentAmmo = magazineSize;
        initialRotation = transform.localRotation;

        if (slideTransform && slideEndTransform)
        {
            slideStartLocalPos = slideTransform.localPosition;
            slideEndLocalPos = slideEndTransform.localPosition;
        }

        // Prevent shooting immediately at start
        nextFireTime = Time.time + 0.1f;
    }

    void Update()
    {
        if (isReloading) return;

        HandleInput();
        HandleWeaponSway();
        HandleWeaponPosition();
        ReturnSlideForward();
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
        nextFireTime = Time.time + (60f / fireRateRPM);

        // Raycast (hitscan)
        if (Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out RaycastHit hit,
            range))
        {
            // hit.collider.GetComponent<Health>()?.TakeDamage(damage);
        }

        // Camera recoil
        playerCamera.transform.Rotate(-recoilKick, 0f, 0f);

        // Slide moves back
        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndLocalPos;

        // Shell casing ejection
        if (shellCasingPrefab && shellEjectPoint)
        {
            GameObject shell = Instantiate(
                shellCasingPrefab,
                shellEjectPoint.position,
                Quaternion.LookRotation(playerCamera.transform.forward)
            );

            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb)
            {
                Vector3 ejectDirection =
                    shellEjectPoint.right * Random.Range(1.5f, 2.5f) +
                    shellEjectPoint.forward * Random.Range(0.2f, 0.4f);

                rb.AddForce(ejectDirection, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
            }

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

    void ReturnSlideForward()
    {
        if (!slideTransform || !slideEndTransform) return;

        slideTransform.localPosition = Vector3.Lerp(
            slideTransform.localPosition,
            slideStartLocalPos,
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

        Vector3 sway = new Vector3(mouseY, -mouseX, 0f) * swayAmount;
        Quaternion targetRotation = Quaternion.Euler(sway);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation * initialRotation,
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
