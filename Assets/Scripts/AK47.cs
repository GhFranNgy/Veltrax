using UnityEngine;
using System.Collections;

public class AK47 : MonoBehaviour
{
    public enum FireMode
    {
        Single,
        Automatic
    }

    [Header("=== FIRE MODE ===")]
    public FireMode fireMode = FireMode.Single;

    [Header("=== KEY BINDINGS ===")]
    public KeyCode fireKey = KeyCode.Mouse0;       // Left Mouse Button
    public KeyCode aimKey = KeyCode.Mouse1;        // Right Mouse Button
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode switchFireModeKey = KeyCode.F;

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
    [SerializeField] private AudioClip reloadSound;
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
    [SerializeField] private float recoilKickBack = 0.1f;      // Gun moves back
    [SerializeField] private float recoilRotation = 5f;        // Gun tilts
    [SerializeField] private float recoilRecoverySpeed = 8f;   // Gun returns smoothly

    [Header("=== SWAY ===")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float swaySmooth = 6f;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float positionLerpSpeed = 8f;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private bool canShoot = true;

    private Quaternion initialWeaponRotation;
    private Vector3 slideStartLocalPos;
    private Vector3 slideEndLocalPos;

    // Physical recoil
    private Vector3 currentRecoilPosition;
    private Vector3 targetRecoilPosition;
    private Vector3 currentRecoilRotation;
    private Vector3 targetRecoilRotation;

    void Start()
    {
        currentAmmo = magazineSize;
        initialWeaponRotation = transform.localRotation;

        if (slideTransform && slideEndTransform)
        {
            slideStartLocalPos = slideTransform.localPosition;
            slideEndLocalPos = slideEndTransform.localPosition;
        }

        nextFireTime = Time.time;
    }

    void Update()
    {
        // Switch fire mode
        if (Input.GetKeyDown(switchFireModeKey))
        {
            fireMode = fireMode == FireMode.Single ? FireMode.Automatic : FireMode.Single;
            Debug.Log("Fire Mode: " + fireMode);
        }

        if (isReloading) return;

        HandleInput();
        HandleWeaponSway();
        HandleWeaponPosition();
        HandlePhysicalRecoil();
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

        // Physical recoil
        targetRecoilPosition += Vector3.back * recoilKickBack;
        targetRecoilRotation += new Vector3(-recoilRotation, Random.Range(-recoilRotation / 2f, recoilRotation / 2f), 0f);

        // Slide / Bolt
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
                Vector3 ejectDir =
                    shellEjectPoint.right * Random.Range(1.8f, 2.6f) +
                    shellEjectPoint.forward * 0.3f;

                rb.AddForce(ejectDir, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
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

        // Sound
        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, shootVolume);
    }

    void HandlePhysicalRecoil()
    {
        // Smoothly move gun back to original position
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // Apply target recoil
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, recoilRecoverySpeed * Time.deltaTime);
        currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, targetRecoilRotation, recoilRecoverySpeed * Time.deltaTime);

        gunHolder.localPosition += currentRecoilPosition;
        gunHolder.localRotation *= Quaternion.Euler(currentRecoilRotation);

        // Reset target recoil for next frame
        targetRecoilPosition = Vector3.zero;
        targetRecoilRotation = Vector3.zero;
    }

    void ReturnSlideForward()
    {
        if (!slideTransform) return;

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

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot * initialWeaponRotation,
            Time.deltaTime * swaySmooth
        );
    }

    void HandleWeaponPosition()
    {
        Transform target = hipPosition;

        bool aiming = Input.GetKey(aimKey);
        bool running = Input.GetKey(runKey) && Input.GetAxis("Vertical") > 0;

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
