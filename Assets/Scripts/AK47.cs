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
    [SerializeField] private AudioClip reloadSound; // reload sound added
    [Range(0f, 1f)] [SerializeField] private float shootVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float reloadVolume = 0.8f;

    [Header("=== WEAPON POSITIONS ===")]
    [SerializeField] private Transform hipPosition;
    [SerializeField] private Transform aimPosition;
    [SerializeField] private Transform runPosition;

    [Header("=== AK-47 STATS ===")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2.45f;
    [SerializeField] private float fireRateRPM = 600f;
    [SerializeField] private float range = 150f;

    [Header("=== RECOIL ===")]
    [SerializeField] private float recoilVertical = 2.2f;
    [SerializeField] private float recoilHorizontal = 1.2f;
    [SerializeField] private float recoilReturnSpeed = 18f;
    [SerializeField] private float recoilSnappiness = 25f;

    [Header("=== SWAY ===")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private bool canShoot = false;

    private Quaternion initialWeaponRotation;
    private Quaternion baseCameraRotation;

    private Vector3 slideStartLocalPos;
    private Vector3 slideEndLocalPos;

    private Vector2 currentRecoil;
    private Vector2 targetRecoil;

    void Start()
    {
        currentAmmo = magazineSize;
        initialWeaponRotation = transform.localRotation;
        baseCameraRotation = playerCamera.transform.localRotation;

        if (slideTransform && slideEndTransform)
        {
            slideStartLocalPos = slideTransform.localPosition;
            slideEndLocalPos = slideEndTransform.localPosition;
        }

        nextFireTime = Time.time + 0.15f;
    }

    void Update()
    {
        if (isReloading) return;

        HandleInput();
        HandleWeaponSway();
        HandleWeaponPosition();
        HandleRecoil();
        ReturnSlideForward();
    }

    void HandleInput()
    {
        if (!Input.GetButton("Fire1"))
            canShoot = true;

        if (Input.GetKeyDown(KeyCode.R))
            StartCoroutine(Reload());

        if (canShoot && Input.GetButton("Fire1") && Time.time >= nextFireTime)
            Shoot();
    }

    void Shoot()
    {
        if (currentAmmo <= 0) return;

        canShoot = false;
        currentAmmo--;
        nextFireTime = Time.time + (60f / fireRateRPM);

        // Recoil
        targetRecoil.x += recoilVertical;
        targetRecoil.y += Random.Range(-recoilHorizontal, recoilHorizontal);

        // Slide
        if (slideTransform && slideEndTransform)
            slideTransform.localPosition = slideEndLocalPos;

        // Shell ejection
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

    void HandleRecoil()
    {
        targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, recoilReturnSpeed * Time.deltaTime);
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, recoilSnappiness * Time.deltaTime);

        playerCamera.transform.localRotation = baseCameraRotation * Quaternion.Euler(-currentRecoil.x, currentRecoil.y, 0f);
    }

    void ReturnSlideForward()
    {
        if (!slideTransform) return;

        slideTransform.localPosition = Vector3.Lerp(slideTransform.localPosition, slideStartLocalPos, Time.deltaTime * slideReturnSpeed);
    }

    IEnumerator Reload()
    {
        if (currentAmmo == magazineSize) yield break;

        isReloading = true;

        // Play reload sound
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

    void HandleWeaponPosition()
    {
        Transform target = hipPosition;

        bool aiming = Input.GetButton("Fire2");
        bool running = Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0;

        if (running)
            target = runPosition;
        else if (aiming)
            target = aimPosition;

        gunHolder.localPosition = Vector3.Lerp(gunHolder.localPosition, target.localPosition, Time.deltaTime * positionLerpSpeed);
        gunHolder.localRotation = Quaternion.Slerp(gunHolder.localRotation, target.localRotation, Time.deltaTime * positionLerpSpeed);
    }
}